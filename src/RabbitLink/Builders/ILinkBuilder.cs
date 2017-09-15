﻿#region Usings

using System;
using RabbitLink.Connection;
using RabbitLink.Logging;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Builders
{
    /// <summary>
    ///     <see cref="Link" /> configuration builder
    /// </summary>
    public interface ILinkBuilder
    {
        /// <summary>
        /// Name of connection
        /// </summary>
        ILinkBuilder ConnectionName(string value);
        
        /// <summary>
        /// Amqp connection string
        /// </summary>
        ILinkBuilder Uri(string value);
        
        /// <summary>
        /// Amqp connection string
        /// </summary>
        ILinkBuilder Uri(Uri value);

        /// <summary>
        ///     Is connection must start automatically
        ///     By default true
        /// </summary>
        ILinkBuilder AutoStart(bool value);

        /// <summary>
        ///     Connection timeout
        ///     By default 10 seconds
        /// </summary>
        ILinkBuilder Timeout(TimeSpan value);

        /// <summary>
        ///     Timeout before next connection attempt
        ///     By default 10 seconds
        /// </summary>
        ILinkBuilder RecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Logger factory
        ///     By default uses <see cref="LinkNullLogger" />
        /// </summary>
        ILinkBuilder LoggerFactory(ILinkLoggerFactory value);

        /// <summary>
        ///     Sets <see cref="LinkMessageProperties.AppId" /> to all published messages, white spaces will be trimmed, must be
        ///     not null or white space
        ///     By default Guit.NewValue().ToString("D")
        /// </summary>
        ILinkBuilder AppId(string value);

        /// <summary>
        /// Sets handler for state changes
        /// </summary>
        ILinkBuilder OnStateChange(LinkStateHandler<LinkConnectionState> handler);
        
        /// <summary>
        /// Use background threads for connection handling.
        /// By default false
        /// </summary>
        ILinkBuilder UseBackgroundThreadsForConnection(bool value);

        /// <summary>
        /// Builds <see cref="ILink"/> instance
        /// </summary>
        ILink Build();
    }
}