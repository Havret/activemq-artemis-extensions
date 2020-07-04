using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.Tests
{
    public class ProducerSpec
    {
        [Fact]
        public async Task Should_register_producer()
        {
            var address1 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(activeMqBuilder =>
            {
                activeMqBuilder.AddProducer<TestProducer>(address1, RoutingType.Anycast);
            });

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();

            Assert.NotNull(testProducer);
        }

        [Fact]
        public async Task Should_send_message_using_registered_producer()
        {
            var address1 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(activeMqBuilder =>
            {
                activeMqBuilder.AddProducer<TestProducer>(address1, RoutingType.Anycast);
            });

            var consumer = await testFixture.Connection.CreateConsumerAsync(address1, RoutingType.Anycast);

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();
            await testProducer.SendMessage("foo");

            var msg = await consumer.ReceiveAsync();
            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}