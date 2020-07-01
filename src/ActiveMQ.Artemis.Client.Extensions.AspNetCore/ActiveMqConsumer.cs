using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalUtils;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqConsumer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AsyncValueLazy<IConsumer> _consumer;
        private readonly Func<Message, IConsumer, IServiceProvider, Task> _handler;
        private Task _task;

        public ActiveMqConsumer(IServiceProvider serviceProvider, Func<CancellationToken, Task<IConsumer>> consumerFactory, Func<Message, IConsumer, IServiceProvider, Task> handler)
        {
            _serviceProvider = serviceProvider;
            _consumer = new AsyncValueLazy<IConsumer>(consumerFactory);
            _handler = handler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var consumer = await _consumer.GetValueAsync(cancellationToken);
            _task = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var msg = await consumer.ReceiveAsync(cancellationToken);
                    await _handler(msg, consumer, _serviceProvider);
                }
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}