using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalUtils;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up ActiveMQ Artemis Client related services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ActiveMqArtemisClientExtensions
    {
        /// <summary>
        /// Adds ActiveMQ Artemis Client and its dependencies to the <paramref name="services"/>, and allows consumers and producers to be configured.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="IConnection"/> to ActiveMQ Artemis.</param>
        /// <param name="endpoints">A list of endpoints that client may use to connect to the broker.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddActiveMq(this IServiceCollection services, string name, IEnumerable<Endpoint> endpoints)
        {
            return services.AddActiveMq(name: name, endpoints: endpoints, configureFactory: null);
        }
        
        /// <summary>
        /// Adds ActiveMQ Artemis Client and its dependencies to the <paramref name="services"/>, and allows consumers and producers to be configured.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="IConnection"/> to ActiveMQ Artemis.</param>
        /// <param name="endpoints">A list of endpoints that client may use to connect to the broker.</param>
        /// <param name="configureFactory">A delegate that is used to configure a <see cref="ConnectionFactory"/>.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddActiveMq(this IServiceCollection services, string name, IEnumerable<Endpoint> endpoints, Action<IServiceProvider, ConnectionFactory> configureFactory)
        {
            var builder = new ActiveMqBuilder(name, services);

            builder.Services.AddOptions<ActiveMqOptions>(name);
            builder.Services.AddHostedService<ActiveMqHostedService>();
            builder.Services.TryAddSingleton<ConnectionProvider>();
            builder.Services.TryAddTransient<ConnectionFactory>();

            builder.Services.AddSingleton(provider =>
            {
                var connectionFactory = provider.GetService<ConnectionFactory>();
                configureFactory?.Invoke(provider, connectionFactory);
                return new NamedConnection(name, token => connectionFactory.CreateAsync(endpoints, token));
            });
            builder.Services.AddSingleton<IActiveMqTopologyManager>(provider =>
            {
                var optionsFactory = provider.GetService<IOptionsFactory<ActiveMqOptions>>();
                var activeMqOptions = optionsFactory.Create(name);
                if (activeMqOptions.EnableQueueDeclaration)
                {
                    var lazyConnection = provider.GetConnection(name);
                    return new ActiveMqTopologyManager(lazyConnection, activeMqOptions.QueueConfigurations.ToList());                    
                }
                else
                {
                    return new NullActiveMqTopologyManager();
                }
            });

            return builder;
        }

        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, Func<Message, IConsumer, IServiceProvider, Task> handler)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.QueueConfigurations.Add(new QueueConfiguration
            {
                Address = address,
                RoutingType = routingType,
                Name = queue,
                AutoCreateAddress = true                
            }));
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

        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address, RoutingType routingType) where TProducer : class
        {
            if (builder.Services.Any(x => x.ServiceType == typeof(TProducer)))
            {
                var message =
                    $"There has already been registered Producer with the type '{typeof(TProducer).FullName}'. " +
                    "Typed Producer must be unique. " +
                    "Consider using inheritance to create multiple unique types with the same API surface.";
                throw new InvalidOperationException(message);
            }

            builder.Services.AddSingleton(provider =>
            {
                return new TypedActiveMqProducer<TProducer>(async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateProducerAsync(address, routingType, token);
                });
            });
            builder.Services.AddSingleton<IProducerInitializer>(provider => provider.GetRequiredService<TypedActiveMqProducer<TProducer>>());
            builder.Services.AddTransient(provider => ActivatorUtilities.CreateInstance<TProducer>(provider, provider.GetRequiredService<TypedActiveMqProducer<TProducer>>()));
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IAnonymousProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddAnonymousProducer<TProducer>(this IActiveMqBuilder builder) where TProducer : class
        {
            if (builder.Services.Any(x => x.ServiceType == typeof(TProducer)))
            {
                var message =
                    $"There has already been registered Anonymous Producer with the type '{typeof(TProducer).FullName}'. " +
                    "Typed Anonymous Producer must be unique. " +
                    "Consider using inheritance to create multiple unique types with the same API surface.";
                throw new InvalidOperationException(message);
            }

            builder.Services.AddSingleton(provider =>
            {
                return new TypedActiveMqAnonymousProducer<TProducer>(async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateAnonymousProducerAsync(token);
                });
            });
            builder.Services.AddSingleton<IProducerInitializer>(provider => provider.GetRequiredService<TypedActiveMqAnonymousProducer<TProducer>>());
            builder.Services.AddTransient(provider => ActivatorUtilities.CreateInstance<TProducer>(provider, provider.GetRequiredService<TypedActiveMqAnonymousProducer<TProducer>>()));
            return builder;
        }
        
        /// <summary>
        /// Configures a queue declaration. If a queue declaration is enabled the client will declare queues on the broker according to the provided configuration.
        /// If the queue doesn't exist it will be created, if the queue does exist it will be updated accordingly.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="enableQueueDeclaration">Specified if a queue declaration is enabled.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder EnableQueueDeclaration(this IActiveMqBuilder builder, bool enableQueueDeclaration = true)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.EnableQueueDeclaration = enableQueueDeclaration);
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