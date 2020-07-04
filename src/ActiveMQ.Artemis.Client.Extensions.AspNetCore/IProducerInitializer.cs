using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal interface IProducerInitializer
    {
        ValueTask Initialize(CancellationToken cancellationToken);
    }
}