using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal interface IProducerInitializer
    {
        ValueTask Initialize(CancellationToken cancellationToken);
    }
}