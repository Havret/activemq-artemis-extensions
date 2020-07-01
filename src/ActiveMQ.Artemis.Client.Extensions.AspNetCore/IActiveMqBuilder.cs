using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    public interface IActiveMqBuilder
    {
        string Name { get; }
        IServiceCollection Services { get; }
    }
}