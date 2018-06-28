﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Adapters
{
    /// <summary>
    /// A mock adapter that can be used for unit testing of bot logic.
    /// </summary>
    /// <seealso cref="TestFlow"/>
    public class TestAdapter : BotAdapter
    {
        private object _conversationLock = new object();
        private object _activeQueueLock = new object();

        private int _nextId = 0;
        private readonly bool sendTraceActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAdapter"/> class.
        /// </summary>
        /// <param name="conversation">A reference to the conversation to begin the adapter state with.</param>
        public TestAdapter(ConversationReference conversation = null, bool sendTraceActivity = false)
        {
            this.sendTraceActivity = sendTraceActivity;
            if (conversation != null)
            {
                Conversation = conversation;
            }
            else
            {
                Conversation = new ConversationReference
                {
                    ChannelId = "test",
                    ServiceUrl = "https://test.com",
                };

                Conversation.User = new ChannelAccount("user1", "User1");
                Conversation.Bot = new ChannelAccount("bot", "Bot");
                Conversation.Conversation = new ConversationAccount(false, "convo1", "Conversation1");
            }
        }

        /// <summary>
        /// Gets the queue of responses from the bot.
        /// </summary>
        /// <value>The queue of responses from the bot.</value>
        public Queue<Activity> ActiveQueue { get; } = new Queue<Activity>();

        /// <summary>
        /// Gets or sets a reference to the current coversation.
        /// </summary>
        /// <value>A reference to the current conversation.</value>
        public ConversationReference Conversation { get; set; }

        /// <summary>
        /// Adds middleware to the adapter's pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        /// <returns>The updated adapter object.</returns>
        /// <remarks>Middleware is added to the adapter at initialization time.
        /// For each turn, the adapter calls middleware in the order in which you added it.
        /// </remarks>
        public new TestAdapter Use(IMiddleware middleware)
        {
            base.Use(middleware);
            return this;
        }

        /// <summary>
        /// Receives an activity and runs it through the middleware pipeline.
        /// </summary>
        /// <param name="activity">The activity to process.</param>
        /// <param name="callback">The bot logic to invoke.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task ProcessActivityAsync(Activity activity, Func<ITurnContext, Task> callback, CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_conversationLock)
            {
                // ready for next reply
                if (activity.Type == null)
                {
                    activity.Type = ActivityTypes.Message;
                }

                activity.ChannelId = Conversation.ChannelId;
                activity.From = Conversation.User;
                activity.Recipient = Conversation.Bot;
                activity.Conversation = Conversation.Conversation;
                activity.ServiceUrl = Conversation.ServiceUrl;

                var id = activity.Id = (_nextId++).ToString();
            }

            if (activity.Timestamp == null || activity.Timestamp == default(DateTime))
            {
                activity.Timestamp = DateTime.UtcNow;
            }

            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends activities to the conversation.
        /// </summary>
        /// <param name="context">The context object for the turn.</param>
        /// <param name="activities">The activities to send.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activities are successfully sent, the task result contains
        /// an array of <see cref="ResourceResponse"/> objects containing the IDs that
        /// the receiving channel assigned to the activities.</remarks>
        /// <seealso cref="ITurnContext.OnSendActivities(SendActivitiesHandler)"/>
        public async override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext context, Activity[] activities, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (activities == null)
            {
                throw new ArgumentNullException(nameof(activities));
            }

            if (activities.Length == 0)
            {
                throw new ArgumentException("Expecting one or more activities, but the array was empty.", nameof(activities));
            }

            var responses = new ResourceResponse[activities.Length];

            // NOTE: we're using for here (vs. foreach) because we want to simultaneously index into the
            // activities array to get the activity to process as well as use that index to assign
            // the response to the responses array and this is the most cost effective way to do that.
            for (var index = 0; index < activities.Length; index++)
            {
                var activity = activities[index];

                if (string.IsNullOrEmpty(activity.Id))
                {
                    activity.Id = Guid.NewGuid().ToString("n");
                }

                if (activity.Timestamp == null)
                {
                    activity.Timestamp = DateTime.UtcNow;
                }

                if (activity.Type == ActivityTypesEx.Delay)
                {
                    // The BotFrameworkAdapter and Console adapter implement this
                    // hack directly in the POST method. Replicating that here
                    // to keep the behavior as close as possible to facillitate
                    // more realistic tests.
                    var delayMs = (int)activity.Value;

                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
                else if (activity.Type == ActivityTypes.Trace)
                {
                    if (sendTraceActivity)
                    {
                        lock (_activeQueueLock)
                        {
                            ActiveQueue.Enqueue(activity);
                        }
                    }
                }
                else
                {
                    lock (_activeQueueLock)
                    {
                        ActiveQueue.Enqueue(activity);
                    }
                }

                responses[index] = new ResourceResponse(activity.Id);
            }

            return responses;
        }

        /// <summary>
        /// Replaces an existing activity in the <see cref="ActiveQueue"/>.
        /// </summary>
        /// <param name="context">The context object for the turn.</param>
        /// <param name="activity">New replacement activity.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activity is successfully sent, the task result contains
        /// a <see cref="ResourceResponse"/> object containing the ID that the receiving
        /// channel assigned to the activity.
        /// <para>Before calling this, set the ID of the replacement activity to the ID
        /// of the activity to replace.</para></remarks>
        /// <seealso cref="ITurnContext.OnUpdateActivity(UpdateActivityHandler)"/>
        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken)
        {
            lock (_activeQueueLock)
            {
                var replies = ActiveQueue.ToList();
                for (int i = 0; i < ActiveQueue.Count; i++)
                {
                    if (replies[i].Id == activity.Id)
                    {
                        replies[i] = activity;
                        ActiveQueue.Clear();
                        foreach (var item in replies)
                        {
                            ActiveQueue.Enqueue(item);
                        }

                        return Task.FromResult(new ResourceResponse(activity.Id));
                    }
                }
            }

            return Task.FromResult(new ResourceResponse());
        }

        /// <summary>
        /// Deletes an existing activity in the <see cref="ActiveQueue"/>.
        /// </summary>
        /// <param name="context">The context object for the turn.</param>
        /// <param name="reference">Conversation reference for the activity to delete.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>The <see cref="ConversationReference.ActivityId"/> of the conversation
        /// reference identifies the activity to delete.</remarks>
        /// <seealso cref="ITurnContext.OnDeleteActivity(DeleteActivityHandler)"/>
        public override Task DeleteActivityAsync(ITurnContext context, ConversationReference reference, CancellationToken cancellationToken)
        {
            lock (_activeQueueLock)
            {
                var replies = ActiveQueue.ToList();
                for (int i = 0; i < ActiveQueue.Count; i++)
                {
                    if (replies[i].Id == reference.ActivityId)
                    {
                        replies.RemoveAt(i);
                        ActiveQueue.Clear();
                        foreach (var item in replies)
                        {
                            ActiveQueue.Enqueue(item);
                        }

                        break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a new conversation on the specified channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel.</param>
        /// <param name="callback">The bot logic to call when the conversation is created.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>This resets the <see cref="ActiveQueue"/>, and does not maintain multiple converstion queues.</remarks>
        public Task CreateConversationAsync(string channelId, Func<ITurnContext, Task> callback, CancellationToken cancellationToken)
        {
            ActiveQueue.Clear();
            var update = Activity.CreateConversationUpdateActivity();
            update.Conversation = new ConversationAccount() { Id = Guid.NewGuid().ToString("n") };
            var context = new TurnContext(this, (Activity)update);
            return callback(context);
        }

        /// <summary>
        /// Dequeues and returns the next bot response from the <see cref="ActiveQueue"/>.
        /// </summary>
        /// <returns>The next activity in the queue; or null, if the queue is empty.</returns>
        /// <remarks>A <see cref="TestFlow"/> object calls this to get the next response from the bot.</remarks>
        public IActivity GetNextReply()
        {
            lock (_activeQueueLock)
            {
                if (ActiveQueue.Count > 0)
                {
                    return ActiveQueue.Dequeue();
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a message activity from text and the current conversational context.
        /// </summary>
        /// <param name="text">The message text.</param>
        /// <returns>An appropriate message activity.</returns>
        /// <remarks>A <see cref="TestFlow"/> object calls this to get a message activity
        /// appropriate to the current conversation.</remarks>
        public Activity MakeActivity(string text = null)
        {
            Activity activity = new Activity
            {
                Type = ActivityTypes.Message,
                From = Conversation.User,
                Recipient = Conversation.Bot,
                Conversation = Conversation.Conversation,
                ServiceUrl = Conversation.ServiceUrl,
                Id = (_nextId++).ToString(),
                Text = text,
            };

            return activity;
        }

        /// <summary>
        /// Processes a message activity from a user.
        /// </summary>
        /// <param name="userSays">The text of the user's message.</param>
        /// <param name="callback">The turn processing logic to use.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <seealso cref="TestFlow.Send(string)"/>
        public Task SendTextToBotAsync(string userSays, Func<ITurnContext, Task> callback, CancellationToken cancellationToken)
        {
            return ProcessActivityAsync(MakeActivity(userSays), callback, cancellationToken);
        }
    }
}
