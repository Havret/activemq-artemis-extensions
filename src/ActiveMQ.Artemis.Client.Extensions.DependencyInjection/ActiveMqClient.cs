using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ActiveMqClient : IActiveMqClient
    {
        private readonly IEnumerable<ActiveMqTopologyManager> _topologyManagers;
        private readonly IEnumerable<ActiveMqConsumer> _consumers;
        private readonly IEnumerable<IProducerInitializer> _producerInitializers;

        public ActiveMqClient(IEnumerable<ActiveMqTopologyManager> topologyManagers, IEnumerable<ActiveMqConsumer> consumers, IEnumerable<IProducerInitializer> producerInitializers)
        {
            _topologyManagers = topologyManagers;
            _consumers = consumers;
            _producerInitializers = producerInitializers;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var producer in _producerInitializers)
            {
                await producer.Initialize(cancellationToken).ConfigureAwait(false);
            }
            
            foreach (var activeMqTopologyManager in _topologyManagers)
            {
                await activeMqTopologyManager.CreateTopologyAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var activeMqConsumer in _consumers)
            {
                await activeMqConsumer.StartAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var activeMqConsumer in _consumers)
            {
                await activeMqConsumer.StopAsync();
            }
        }
    }
}