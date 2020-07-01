using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqBuilder : IActiveMqBuilder
    {
        public ActiveMqBuilder(string name, IServiceCollection services)
        {
            Name = name;
            Services = services;
        }

        public string Name { get; }
        public IServiceCollection Services { get; }
    }
}