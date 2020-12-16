﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;

namespace Microsoft.Bot.Builder
{
    /// <summary>
    /// A factory class used to create ConnectorClients with appropriate credentials for the current appId.
    /// </summary>
    public abstract class ConnectorFactory
    {
        /// <summary>
        /// A factort method used to create <see cref="IConnectorClient"/> instances.
        /// </summary>
        /// <param name="serviceUrl">The url for the client.</param>
        /// <param name="audience">The audience for the credentails the client will use.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A Task of <see cref="IConnectorClient"/>.</returns>
        public abstract Task<IConnectorClient> CreateAsync(string serviceUrl, string audience, CancellationToken cancellationToken);
    }
}
