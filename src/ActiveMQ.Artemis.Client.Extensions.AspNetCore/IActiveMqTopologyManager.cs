using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal interface IActiveMqTopologyManager
    {
        Task CreateTopologyAsync(CancellationToken cancellationToken);
    }
}