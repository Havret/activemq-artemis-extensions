﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalUtils;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ActiveMQArtemisClientExtensions
    {
        public static IActiveMqBuilder AddActiveMq(this IServiceCollection services, string name = "")
        {
            services.AddHostedService<ActiveMqHostedService>();
            services.TryAddSingleton<ConnectionProvider>();
            services.TryAddTransient<ConnectionFactory>();
            services.TryAddSingleton(new TopologyRegistry());
            services.AddSingleton(provider =>
            {
                var connectionFactory = provider.GetService<ConnectionFactory>();
                var endpoint = Endpoint.Create(host: "localhost", port: 5672, "guest", "guest");
                return new NamedConnection(name, token => connectionFactory.CreateAsync(endpoint, token));
            });
            services.AddSingleton(provider =>
            {
                var lazyConnection = provider.GetConnection(name);
                var topologyRegister = provider.GetService<TopologyRegistry>();
                if (!topologyRegister.NamedQueueConfigurations.TryGetValue(name, out var queueConfigurations))
                {
                    queueConfigurations = new List<QueueConfiguration>(0);
                }
                return new ActiveMqTopologyManager(lazyConnection, queueConfigurations);
            });
            var registry = (TopologyRegistry)services.Single(sd => sd.ServiceType == typeof(TopologyRegistry)).ImplementationInstance;
            
            registry.NamedQueueConfigurations[name] = new List<QueueConfiguration>();
            
            return new ActiveMqBuilder(name, services);
        }

        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, Func<Message, IConsumer, IServiceProvider, Task> handler)
        {
            var register = (TopologyRegistry) builder.Services.Single(x => x.ServiceType == typeof(TopologyRegistry)).ImplementationInstance;
            if (register.NamedQueueConfigurations.TryGetValue(builder.Name, out var queueConfigurations))
            {
                queueConfigurations.Add(new QueueConfiguration
                {
                    Address = address,
                    RoutingType = routingType,
                    Name = queue,
                    AutoCreateAddress = true
                });
            }

            return builder.AddConsumer(new ConsumerConfiguration
            {
                Address = address,
                Queue = queue
            }, handler);
        }
        
        private static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, ConsumerConfiguration consumerConfiguration, Func<Message, IConsumer, IServiceProvider, Task> handler)
        {
            builder.Services.AddSingleton(provider =>
            {
                return new ActiveMqConsumer(provider, async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateConsumerAsync(consumerConfiguration, token);
                }, handler);
            });
            return builder;
        }
        
        public static IActiveMqBuilder AddProducer<T>(this IActiveMqBuilder builder, string address, RoutingType routingType) where T : class
        {
            builder.AddProducer(address, routingType);
            builder.Services.AddSingleton(provider => ActivatorUtilities.CreateInstance<T>(provider, provider.GetRequiredService<IProducer>()));
            return builder;
        }
        
        public static IActiveMqBuilder AddProducer(this IActiveMqBuilder builder, string address, RoutingType routingType)
        {
            builder.Services.AddSingleton<IProducer>(provider =>
            {
                return new ActiveMqProducer(async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateProducerAsync(address, routingType, token);
                });
            });
            return builder;
        }

        private static ValueTask<IConnection> GetConnection(this IServiceProvider serviceProvider, string name, CancellationToken cancellationToken)
        {
            return serviceProvider.GetService<ConnectionProvider>().GetConnection(name, cancellationToken);
        }

        private static AsyncValueLazy<IConnection> GetConnection(this IServiceProvider serviceProvider, string name)
        {
            return serviceProvider.GetService<ConnectionProvider>().GetConnection(name);
        }
    }
}