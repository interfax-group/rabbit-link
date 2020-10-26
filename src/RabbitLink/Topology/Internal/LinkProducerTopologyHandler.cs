using System;
using System.Threading.Tasks;

namespace RabbitLink.Topology.Internal
{
    internal class LinkProducerTopologyHandler : ILinkProducerTopologyHandler
    {
        private readonly LinkProducerTopologyConfigDelegate _configAction;
        private readonly LinkTopologyErrorDelegate _errorAction;

        public LinkProducerTopologyHandler(
            LinkProducerTopologyConfigDelegate configAction,
            LinkTopologyErrorDelegate errorAction
        )
        {
            _configAction = configAction ?? throw new ArgumentNullException(nameof(configAction));
            _errorAction = errorAction ?? throw new ArgumentNullException(nameof(errorAction));
        }

        public Task<ILinkExchange> Configure(ILinkTopologyConfig config)
        {
            return _configAction(config);
        }

        public Task ConfigurationError(Exception ex)
        {
            return _errorAction(ex);
        }
    }
}
