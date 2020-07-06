using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class NullActiveMqTopologyManager : IActiveMqTopologyManager
    {
        public Task CreateTopologyAsync(CancellationToken cancellationToken)
        {
            // do nothing;
            return Task.CompletedTask;
        }
    }
}