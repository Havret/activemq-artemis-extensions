using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtemisNetClient.Examples.ConsoleApplication
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddActiveMq(name: "my-artemis-cluster", endpoints: new[] { Endpoint.Create(host: "localhost", port: 5672, "guest", "guest") })
                    .ConfigureConnectionFactory((provider, factory) =>
                    {
                        factory.LoggerFactory = LoggerFactory.Create(builder =>
                        {
                            builder.SetMinimumLevel(LogLevel.Information);
                            builder.AddConsole();
                        });
                    })
                    .AddConsumer("a11", RoutingType.Anycast, async (message, consumer, _, _) =>
                    {
                        Console.WriteLine("a11: " + message.GetBody<string>());
                        await consumer.AcceptAsync(message);
                    })
                    .AddProducer<MyTypedMessageProducer>("a11", RoutingType.Anycast);

            var serviceProvider = services.BuildServiceProvider();
            var activeMqClient = serviceProvider.GetRequiredService<IActiveMqClient>();
            await activeMqClient.StartAsync(default);
            
            var messageProducer = serviceProvider.GetRequiredService<MyTypedMessageProducer>();
            var _ = Task.Run(async () =>
            {
                while (true)
                {
                    await messageProducer.SendTextAsync("text");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
            await activeMqClient.StopAsync(default);
        }
    }
}