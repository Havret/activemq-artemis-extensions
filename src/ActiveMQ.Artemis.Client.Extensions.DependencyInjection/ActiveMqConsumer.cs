using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ActiveMqConsumer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ContextualReceiveObservable _receiveObservable;
        private readonly AsyncValueLazy<IConsumer> _consumer;
        private readonly Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> _handler;
        private Task _task;
        private CancellationTokenSource _cts;
        private readonly ILogger<ActiveMqConsumer> _logger;

        public ActiveMqConsumer(IServiceProvider serviceProvider,
            ContextualReceiveObservable receiveObservable,
            Func<CancellationToken, Task<IConsumer>> consumerFactory,
            Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            _serviceProvider = serviceProvider;
            _receiveObservable = receiveObservable;
            _logger = serviceProvider.GetService<ILogger<ActiveMqConsumer>>();
            _consumer = new AsyncValueLazy<IConsumer>(consumerFactory);
            _handler = handler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;
            var consumer = await _consumer.GetValueAsync(cancellationToken);
            _task = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var msg = await consumer.ReceiveAsync(token).ConfigureAwait(false);
                        _receiveObservable.PreReceive(msg);
                        await _handler(msg, consumer, _serviceProvider, token).ConfigureAwait(false);
                        _receiveObservable.PostReceive(msg);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, string.Empty);
                    }
                }
            }, token);
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            await _task.ConfigureAwait(false);
            _cts.Dispose();
        }
    }
}