using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.Tests
{
    public class ConsumerSpec
    {
        [Fact]
        public async Task Should_create_multiple_concurrent_consumers()
        {
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            var consumers = new ConcurrentBag<IConsumer>();
            var messages = new ConcurrentBag<Message>();
            
            async Task MessageHandler(Message message, IConsumer consumer, IServiceProvider provider)
            {
                consumers.Add(consumer);
                messages.Add(message);
                await consumer.AcceptAsync(message);
            }
            
            await using var testFixture = await TestFixture.CreateAsync(builder =>
            {
                builder.AddConsumer(address, RoutingType.Multicast, queue, new ConsumerOptions { ConcurrentConsumers = 3 }, MessageHandler)
                       .EnableAddressDeclaration()
                       .EnableQueueDeclaration();
            });

            await using var producer = await testFixture.Connection.CreateProducerAsync(address, RoutingType.Multicast);
            for (int i = 0; i < 100; i++)
            {
                await producer.SendAsync(new Message("foo" + i));
            }
            Assert.Equal(3, consumers.Distinct().Count());
            Assert.Equal(100, messages.Count);
        }
    }
}