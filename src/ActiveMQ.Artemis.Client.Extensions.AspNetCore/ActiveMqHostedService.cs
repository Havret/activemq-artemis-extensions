using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqHostedService : IHostedService
    {
        private readonly IEnumerable<ActiveMqTopologyManager> _topologyManagers;
        private readonly IEnumerable<ActiveMqConsumer> _consumers;

        public ActiveMqHostedService(IEnumerable<ActiveMqTopologyManager> topologyManagers, IEnumerable<ActiveMqConsumer> consumers)
        {
            _topologyManagers = topologyManagers;
            _consumers = consumers;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var activeMqTopologyManager in _topologyManagers)
            {
                await activeMqTopologyManager.CreateTopologyAsync(cancellationToken);
            }

            foreach (var activeMqConsumer in _consumers)
            {
                await activeMqConsumer.StartAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var activeMqConsumer in _consumers)
            {
                await activeMqConsumer.StopAsync(cancellationToken);
            }
        }
    }
}