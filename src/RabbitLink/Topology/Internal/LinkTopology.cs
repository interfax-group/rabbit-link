#region Usings

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Connection;
using RabbitLink.Internals;
using RabbitLink.Internals.Async;
using RabbitLink.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkTopology : AsyncStateMachine<LinkTopologyState>, ILinkTopologyInternal, ILinkChannelHandler
    {
        #region Fields

        private readonly ILinkChannel _channel;
        private readonly LinkTopologyConfiguration _configuration;
        private readonly ILinkLogger _logger;
        private readonly object _sync = new object();
        private readonly LinkTopologyRunner<object> _topologyRunner;

        private volatile TaskCompletionSource<object> _readyCompletion =
            new TaskCompletionSource<object>();

        #endregion

        #region Ctor

        public LinkTopology(ILinkChannel channel, LinkTopologyConfiguration configuration)
            : base(LinkTopologyState.Init)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _configuration = configuration;

            _logger = _channel.Connection.Configuration.LoggerFactory.CreateLogger(GetType().Name, Id.ToString("D"))
                      ?? throw new InvalidOperationException("Cannot create logger");

            _topologyRunner = new LinkTopologyRunner<object>(_logger, async cfg =>
            {
                await _configuration.TopologyHandler.Configure(cfg)
                    .ConfigureAwait(false);
                return null;
            });

            _channel.Disposed += ChannelOnDisposed;

            _logger.Debug($"Created(channelId: {_channel.Id})");

            _channel.Initialize(this);
        }

        #endregion

        #region ILinkChannelHandler Members

        public async Task OnActive(IModel model, CancellationToken cancellation)
        {
            var newState = State;

            while (true)
            {
                if (cancellation.IsCancellationRequested)
                {
                    newState = LinkTopologyState.Stopping;
                }

                ChangeState(newState);

                switch (State)
                {
                    case LinkTopologyState.Init:
                        newState = LinkTopologyState.Configuring;
                        break;
                    case LinkTopologyState.Configuring:
                    case LinkTopologyState.Reconfiguring:
                        newState = await OnConfigureAsync(model, State == LinkTopologyState.Reconfiguring,
                                cancellation)
                            .ConfigureAwait(false);
                        break;
                    case LinkTopologyState.Ready:
                        _readyCompletion.TrySetResult(null);

                        try
                        {
                            await cancellation.WaitCancellation()
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            _readyCompletion =
                                new TaskCompletionSource<object>();
                        }

                        newState = LinkTopologyState.Stopping;
                        break;
                    case LinkTopologyState.Disposed:
#pragma warning disable 4014
                        Task.Factory.StartNew(Dispose, TaskCreationOptions.LongRunning);
#pragma warning restore 4014
                        return;
                    case LinkTopologyState.Stopping:
                        if (cancellation.IsCancellationRequested)
                        {
                            ChangeState(LinkTopologyState.Init);
                            return;
                        }
                        newState = LinkTopologyState.Reconfiguring;
                        return;
                    default:
                        throw new NotImplementedException($"Handler for state ${State} not implemeted");
                }
            }
        }

        protected override void OnStateChange(LinkTopologyState newState)
        {
            _logger.Debug($"State change {State} -> {newState}");

            base.OnStateChange(newState);
        }

        public Task OnConnecting(CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        public void MessageAck(BasicAckEventArgs info)
        {
        }

        public void MessageNack(BasicNackEventArgs info)
        {
        }

        public void MessageReturn(BasicReturnEventArgs info)
        {
        }

        #endregion

        #region ILinkTopology Members

        public event EventHandler Disposed;


        public void Dispose()
        {
            Dispose(false);
        }


        public Guid Id { get; } = Guid.NewGuid();

        public Task WaitReadyAsync(CancellationToken? cancellation = null)
        {
            return _readyCompletion.Task
                .ContinueWith(
                    t => t.Result,
                    cancellation ?? CancellationToken.None,
                    TaskContinuationOptions.RunContinuationsAsynchronously,
                    TaskScheduler.Current
                );
        }

        #endregion

        private async Task<LinkTopologyState> OnConfigureAsync(IModel model, bool retry, CancellationToken cancellation)
        {
            if (retry)
            {
                try
                {
                    _logger.Debug($"Retrying in {_configuration.RecoveryInterval.TotalSeconds:0.###}s");
                    await Task.Delay(_configuration.RecoveryInterval, cancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return LinkTopologyState.Reconfiguring;
                }
            }

            _logger.Debug("Configuring topology");

            try
            {
                await _topologyRunner
                    .RunAsync(model, cancellation)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Exception on configuration: {ex}");

                try
                {
                    await _configuration.TopologyHandler.ConfigurationError(ex)
                        .ConfigureAwait(false);
                }
                catch (Exception handlerException)
                {
                    _logger.Error($"Error in error handler: {handlerException}");
                }

                return LinkTopologyState.Reconfiguring;
            }

            try
            {
                await _configuration.TopologyHandler.Ready()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ready handler: {ex}");
            }

            _logger.Debug("Topology configured");

            return LinkTopologyState.Ready;
        }

        private void Dispose(bool byChannel)
        {
            if (State == LinkTopologyState.Disposed)
                return;

            lock (_sync)
            {
                if (State == LinkTopologyState.Disposed)
                    return;

                _logger.Debug($"Disposing ( by channel: {byChannel} )");

                _channel.Disposed -= ChannelOnDisposed;
                if (!byChannel)
                {
                    _channel.Dispose();
                }

                ChangeState(LinkTopologyState.Disposed);

                _readyCompletion.TrySetException(new ObjectDisposedException(GetType().Name));

                _logger.Debug("Disposed");
                _logger.Dispose();

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }


        private void ChannelOnDisposed(object sender, EventArgs eventArgs)
        {
            Dispose(true);
        }
    }
}
