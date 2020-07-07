using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalUtils;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqTopologyManager : IActiveMqTopologyManager
    {
        private readonly AsyncValueLazy<IConnection> _lazyConnection;
        private readonly List<QueueConfiguration> _queueConfigurations;
        private readonly Dictionary<string, HashSet<RoutingType>> _addressConfigurations;

        public ActiveMqTopologyManager(AsyncValueLazy<IConnection> lazyConnection, List<QueueConfiguration> queueConfigurations, Dictionary<string, HashSet<RoutingType>> addressConfigurations)
        {
            _lazyConnection = lazyConnection;
            _queueConfigurations = queueConfigurations;
            _addressConfigurations = addressConfigurations;
        }

        public async Task CreateTopologyAsync(CancellationToken cancellationToken)
        {
            if (_queueConfigurations.Count == 0 && _addressConfigurations.Count == 0)
            {
                return;
            }

            var connection = await _lazyConnection.GetValueAsync(cancellationToken).ConfigureAwait(false);
            await using var topologyManager = await connection.CreateTopologyManagerAsync(cancellationToken).ConfigureAwait(false);

            foreach (var addressConfiguration in _addressConfigurations)
            {
                await topologyManager.DeclareAddressAsync(addressConfiguration.Key, addressConfiguration.Value, cancellationToken);
            }
            
            var queues = await topologyManager.GetQueueNamesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var queueConfiguration in _queueConfigurations.Where(x => !queues.Contains(x.Name)))
            {
                await topologyManager.CreateQueueAsync(queueConfiguration, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}