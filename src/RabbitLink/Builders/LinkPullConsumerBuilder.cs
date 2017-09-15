﻿#region Usings

using System;
using System.Threading;
using RabbitLink.Connection;
using RabbitLink.Consumer;
using RabbitLink.Topology;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkPullConsumerBuilder : ILinkPullConsumerBuilder
    {
        #region Fields

        private readonly ILinkConsumerBuilder _consumerBuilder;
        private readonly TimeSpan _getMessageTimeout;

        #endregion

        #region Ctor

        public LinkPullConsumerBuilder(
            ILinkConsumerBuilder consumerBuilder,
            TimeSpan? getMessageTimeout = null
        )
        {
            _consumerBuilder = consumerBuilder ?? throw new ArgumentNullException(nameof(consumerBuilder));
            _getMessageTimeout = getMessageTimeout ?? Timeout.InfiniteTimeSpan;
        }

        private LinkPullConsumerBuilder(
            LinkPullConsumerBuilder prev,
            ILinkConsumerBuilder consumerBuilder = null,
            TimeSpan? getMessageTimeout = null
        ) : this(
            consumerBuilder ?? prev._consumerBuilder,
            getMessageTimeout ?? prev._getMessageTimeout
        )
        {
        }

        #endregion

        #region ILinkPullConsumerBuilder Members

        public ILinkPullConsumer Build()
        {
            return new LinkPullConsumer(_consumerBuilder, _getMessageTimeout);
        }

        public ILinkPullConsumerBuilder RecoveryInterval(TimeSpan value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.RecoveryInterval(value));
        }

        public ILinkPullConsumerBuilder PrefetchCount(ushort value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.PrefetchCount(value));
        }

        public ILinkPullConsumerBuilder AutoAck(bool value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.AutoAck(value));
        }

        public ILinkPullConsumerBuilder Priority(int value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Priority(value));
        }

        public ILinkPullConsumerBuilder CancelOnHaFailover(bool value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.CancelOnHaFailover(value));
        }

        public ILinkPullConsumerBuilder Exclusive(bool value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Exclusive(value));
        }

        public ILinkPullConsumerBuilder OnStateChange(LinkStateHandler<LinkConsumerState> value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.OnStateChange(value));
        }

        public ILinkPullConsumerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.OnChannelStateChange(value));
        }

        public ILinkPullConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Queue(config));
        }

        public ILinkPullConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config,
            LinkTopologyErrorDelegate error)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Queue(config, error));
        }

        public ILinkPullConsumerBuilder Queue(ILinkConsumerTopologyHandler handler)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Queue(handler));
        }

        public ILinkPullConsumerBuilder GetMessageTimeout(TimeSpan value)
        {
            if (value < TimeSpan.Zero && value!= Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater or equal Zero or equal Timeout.InfiniteTimeSpan");

            return new LinkPullConsumerBuilder(this, getMessageTimeout: value);
        }

        #endregion
    }
}