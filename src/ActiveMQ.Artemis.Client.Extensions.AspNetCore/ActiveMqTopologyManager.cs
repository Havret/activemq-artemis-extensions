using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalUtils;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqTopologyManager
    {
        private readonly AsyncValueLazy<IConnection> _lazyConnection;
        private readonly List<QueueConfiguration> _queueConfigurations;

        public ActiveMqTopologyManager(AsyncValueLazy<IConnection> lazyConnection, List<QueueConfiguration> queueConfigurations)
        {
            _lazyConnection = lazyConnection;
            _queueConfigurations = queueConfigurations;
        }

        public async Task CreateTopologyAsync(CancellationToken cancellationToken)
        {
            if (_queueConfigurations.Count == 0)
            {
                return;
            }

            var connection = await _lazyConnection.GetValueAsync(cancellationToken);
            await using var topologyManager = await connection.CreateTopologyManagerAsync(cancellationToken);
            foreach (var queueConfiguration in _queueConfigurations)
            {
                await topologyManager.CreateQueueAsync(queueConfiguration, cancellationToken);
            }
        }
    }
}