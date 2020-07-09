using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalExtensions;
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
            var builder = new ActiveMqBuilder(name, services);

            builder.Services.AddOptions<ActiveMqOptions>(name);
            builder.Services.AddHostedService<ActiveMqHostedService>();
            builder.Services.TryAddSingleton<ConnectionProvider>();
            builder.Services.TryAddTransient<ConnectionFactory>();

            builder.Services.AddSingleton(provider =>
            {
                var optionsFactory = provider.GetService<IOptionsFactory<ActiveMqOptions>>();
                var activeMqOptions = optionsFactory.Create(name);
                
                var connectionFactory = provider.GetService<ConnectionFactory>();
                foreach (var connectionFactoryAction in activeMqOptions.ConnectionFactoryActions)
                {
                    connectionFactoryAction(provider, connectionFactory);
                }
                return new NamedConnection(name, token => connectionFactory.CreateAsync(endpoints, token));
            });
            builder.Services.AddSingleton<IActiveMqTopologyManager>(provider =>
            {
                var optionsFactory = provider.GetService<IOptionsFactory<ActiveMqOptions>>();
                var activeMqOptions = optionsFactory.Create(name);
                var queueConfigurations = activeMqOptions.EnableQueueDeclaration ? activeMqOptions.QueueConfigurations : new List<QueueConfiguration>(0);
                var addressConfigurations = activeMqOptions.EnableAddressDeclaration ? activeMqOptions.AddressConfigurations : new Dictionary<string, HashSet<RoutingType>>(0);
                var lazyConnection = provider.GetConnection(name);
                return new ActiveMqTopologyManager(lazyConnection, queueConfigurations, addressConfigurations);
            });

            return builder;
        }

        /// <summary>
        /// Adds action to configure to configure a <see cref="ConnectionFactory"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="configureFactoryAction">A delegate that is used to configure a <see cref="ConnectionFactory"/>.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder ConfigureConnectionFactory(this IActiveMqBuilder builder, Action<IServiceProvider, ConnectionFactory> configureFactoryAction)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.ConnectionFactoryActions.Add(configureFactoryAction));
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
        {
            return builder.AddConsumer(address, routingType, new ConsumerOptions(), handler);
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="consumerOptions">The <see cref="IConsumer"/> configuration.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, ConsumerOptions consumerOptions,
            Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
        {
            for (int i = 0; i < consumerOptions.ConcurrentConsumers; i++)
            {
                builder.AddConsumer(new ConsumerConfiguration
                {
                    Address = address,
                    RoutingType = routingType,
                    Credit = consumerOptions.Credit,
                    FilterExpression = consumerOptions.FilterExpression,
                    NoLocalFilter = consumerOptions.NoLocalFilter
                }, handler);
            }

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
        {
            return builder.AddConsumer(address, routingType, queue, new ConsumerOptions(), new QueueOptions(), handler);
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="consumerOptions">The <see cref="IConsumer"/> configuration.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, ConsumerOptions consumerOptions,
            Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
        {
            return builder.AddConsumer(address, routingType, queue, consumerOptions, new QueueOptions(), handler);
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="queueOptions">The queue configuration that will be used when queue declaration is enabled.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, QueueOptions queueOptions,
            Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
        {
            return builder.AddConsumer(address, routingType, queue, new ConsumerOptions(), queueOptions, handler);
        }
        
        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="consumerOptions">The <see cref="IConsumer"/> configuration.</param>
        /// <param name="queueOptions">The queue configuration that will be used when queue declaration is enabled.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, ConsumerOptions consumerOptions, QueueOptions queueOptions,
            Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options =>
            {
                options.QueueConfigurations.Add(new QueueConfiguration
                {
                    Address = address,
                    RoutingType = routingType,
                    Name = queue,
                    Exclusive = queueOptions.Exclusive,
                    FilterExpression = queueOptions.FilterExpression,
                    GroupBuckets = queueOptions.GroupBuckets,
                    GroupRebalance = queueOptions.GroupRebalance,
                    MaxConsumers = queueOptions.MaxConsumers,
                    AutoCreateAddress = queueOptions.AutoCreateAddress,
                    PurgeOnNoConsumers = queueOptions.PurgeOnNoConsumers
                });
                if (options.AddressConfigurations.TryGetValue(address, out var routingTypes))
                {
                    routingTypes.Add(routingType);
                }
                else
                {
                    options.AddressConfigurations[address] = new HashSet<RoutingType> { routingType };
                }
            });
            for (int i = 0; i < consumerOptions.ConcurrentConsumers; i++)
            {
                builder.AddConsumer(new ConsumerConfiguration
                {
                    Address = address,
                    Queue = queue,
                    Credit = consumerOptions.Credit,
                    FilterExpression = consumerOptions.FilterExpression,
                    NoLocalFilter = consumerOptions.NoLocalFilter,
                }, handler);
            }

            return builder;
        }

        private static void AddConsumer(this IActiveMqBuilder builder, ConsumerConfiguration consumerConfiguration, Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
        {
            builder.Services.AddSingleton(provider =>
            {
                return new ActiveMqConsumer(provider, async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateConsumerAsync(consumerConfiguration, token);
                }, handler);
            });
        }
        
        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address) where TProducer : class
        {
            var producerConfiguration = new ProducerConfiguration
            {
                Address = address,
            };
            return builder.AddProducer<TProducer>(producerConfiguration);
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
            var producerConfiguration = new ProducerConfiguration
            {
                Address = address,
                RoutingType = routingType,
            };
            return builder.AddProducer<TProducer>(producerConfiguration);
        }
        
        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="producerOptions">The <see cref="IProducer"/> configuration.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address, RoutingType routingType, ProducerOptions producerOptions) where TProducer : class
        {
            var producerConfiguration = producerOptions.ToConfiguration();
            producerConfiguration.Address = address;
            producerConfiguration.RoutingType = routingType;
            
            return builder.AddProducer<TProducer>(producerConfiguration);
        }
        
        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="producerOptions">The <see cref="IProducer"/> configuration.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address, ProducerOptions producerOptions) where TProducer : class
        {
            var producerConfiguration = producerOptions.ToConfiguration();
            producerConfiguration.Address = address;
            
            return builder.AddProducer<TProducer>(producerConfiguration);
        }

        private static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, ProducerConfiguration producerConfiguration) where TProducer : class
        {
            if (builder.Services.Any(x => x.ServiceType == typeof(TProducer)))
            {
                var message =
                    $"There has already been registered Producer with the type '{typeof(TProducer).FullName}'. " +
                    "Typed Producer must be unique. " +
                    "Consider using inheritance to create multiple unique types with the same API surface.";
                throw new InvalidOperationException(message);
            }

            builder.Services.Configure<ActiveMqOptions>(builder.Name, options =>
            {
                if (!options.AddressConfigurations.TryGetValue(producerConfiguration.Address, out var routingTypes))
                {
                    routingTypes = new HashSet<RoutingType>();
                    options.AddressConfigurations.Add(producerConfiguration.Address, routingTypes);
                }
                if (producerConfiguration.RoutingType.HasValue)
                {
                    routingTypes.Add(producerConfiguration.RoutingType.Value);    
                }
                else
                {
                    routingTypes.Add(RoutingType.Anycast);
                    routingTypes.Add(RoutingType.Multicast);
                }
            });
            
            builder.Services.AddSingleton(provider =>
            {
                return new TypedActiveMqProducer<TProducer>(async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateProducerAsync(producerConfiguration, token);
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
            var anonymousProducerConfiguration = new AnonymousProducerConfiguration();
            return builder.AddAnonymousProducer<TProducer>(anonymousProducerConfiguration);
        }

        /// <summary>
        /// Adds the <see cref="IAnonymousProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="producerOptions">The <see cref="IAnonymousProducer"/> configuration.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddAnonymousProducer<TProducer>(this IActiveMqBuilder builder, ProducerOptions producerOptions) where TProducer : class
        {
            var anonymousProducerConfiguration = new AnonymousProducerConfiguration
            {
                MessagePriority = producerOptions.MessagePriority,
                MessageDurabilityMode = producerOptions.MessageDurabilityMode,
                MessageIdPolicy = producerOptions.MessageIdPolicy,
                SetMessageCreationTime = producerOptions.SetMessageCreationTime
            };
            return builder.AddAnonymousProducer<TProducer>(anonymousProducerConfiguration);
        }

        private static IActiveMqBuilder AddAnonymousProducer<TProducer>(this IActiveMqBuilder builder, AnonymousProducerConfiguration producerConfiguration) where TProducer : class
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
                    var connection = await provider.GetConnection(builder.Name, token).ConfigureAwait(false);
                    return await connection.CreateAnonymousProducerAsync(producerConfiguration, token).ConfigureAwait(false);
                });
            });
            builder.Services.AddSingleton<IProducerInitializer>(provider => provider.GetRequiredService<TypedActiveMqAnonymousProducer<TProducer>>());
            builder.Services.AddTransient(provider => ActivatorUtilities.CreateInstance<TProducer>(provider, provider.GetRequiredService<TypedActiveMqAnonymousProducer<TProducer>>()));
            return builder;
        }
        
        /// <summary>
        /// Configures a queue declaration. If a queue declaration is enabled, the client will declare queues on the broker according to the provided configuration
        /// If the queue doesn't exist, it will be created. If the queue does exist, it will be updated accordingly.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="enableQueueDeclaration">Specified if a queue declaration is enabled.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder EnableQueueDeclaration(this IActiveMqBuilder builder, bool enableQueueDeclaration = true)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.EnableQueueDeclaration = enableQueueDeclaration);
            return builder;
        }

        /// <summary>
        /// Configures an address declaration. If an address declaration is enabled, the client will declare addresses on the broker according to the provided confided configuration.
        /// If the address doesn't exist, it will be created. If the address does exist, it will be updated accordingly.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="enableAddressDeclaration">Specified if an address declaration is enabled.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder EnableAddressDeclaration(this IActiveMqBuilder builder, bool enableAddressDeclaration = true)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.EnableAddressDeclaration = enableAddressDeclaration);
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