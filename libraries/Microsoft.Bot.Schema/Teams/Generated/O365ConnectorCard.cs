// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Bot.Schema.Teams
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// O365 connector card
    /// </summary>
    public partial class O365ConnectorCard
    {
        /// <summary>
        /// Initializes a new instance of the O365ConnectorCard class.
        /// </summary>
        public O365ConnectorCard()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the O365ConnectorCard class.
        /// </summary>
        /// <param name="title">Title of the item</param>
        /// <param name="text">Text for the card</param>
        /// <param name="summary">Summary for the card</param>
        /// <param name="themeColor">Theme color for the card</param>
        /// <param name="sections">Set of sections for the current card</param>
        /// <param name="potentialAction">Set of actions for the current
        /// card</param>
        public O365ConnectorCard(string title = default(string), string text = default(string), string summary = default(string), string themeColor = default(string), IList<O365ConnectorCardSection> sections = default(IList<O365ConnectorCardSection>), IList<O365ConnectorCardActionBase> potentialAction = default(IList<O365ConnectorCardActionBase>))
        {
            Title = title;
            Text = text;
            Summary = summary;
            ThemeColor = themeColor;
            Sections = sections;
            PotentialAction = potentialAction;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets title of the item
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets text for the card
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets summary for the card
        /// </summary>
        [JsonProperty(PropertyName = "summary")]
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets theme color for the card
        /// </summary>
        [JsonProperty(PropertyName = "themeColor")]
        public string ThemeColor { get; set; }

        /// <summary>
        /// Gets or sets set of sections for the current card
        /// </summary>
        [JsonProperty(PropertyName = "sections")]
        public IList<O365ConnectorCardSection> Sections { get; set; }

        /// <summary>
        /// Gets or sets set of actions for the current card
        /// </summary>
        [JsonProperty(PropertyName = "potentialAction")]
        public IList<O365ConnectorCardActionBase> PotentialAction { get; set; }

    }
}
