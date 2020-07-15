using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqConsumer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ContextualReceiveObservable _receiveObservable;
        private readonly AsyncValueLazy<IConsumer> _consumer;
        private readonly Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> _handler;
        private Task _task;
        private CancellationTokenSource _cts;
        private readonly ILogger<ActiveMqConsumer> _logger;

        public ActiveMqConsumer(IServiceProvider serviceProvider,
            ContextualReceiveObservable receiveObservable,
            Func<CancellationToken, Task<IConsumer>> consumerFactory,
            Func<Message, IConsumer, CancellationToken, IServiceProvider, Task> handler)
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
            var consumer = await _consumer.GetValueAsync(cancellationToken);
            _task = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var msg = await consumer.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                        _receiveObservable.PreReceive(msg);
                        await _handler(msg, consumer, _cts.Token, _serviceProvider).ConfigureAwait(false);
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
            }, cancellationToken);
        }

        public Task StopAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            return _task;
        }
    }
}