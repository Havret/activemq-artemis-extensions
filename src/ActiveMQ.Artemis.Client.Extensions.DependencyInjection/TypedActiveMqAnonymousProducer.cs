using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class TypedActiveMqAnonymousProducer<T> : IAnonymousProducer, IProducerInitializer
    {
        private IAnonymousProducer _producer;
        private readonly Func<CancellationToken, Task<IAnonymousProducer>> _producerFactory;
        private readonly SendObservable _sendObservable;

        public TypedActiveMqAnonymousProducer(Func<CancellationToken, Task<IAnonymousProducer>> producerFactory, SendObservable sendObservable)
        {
            _producerFactory = producerFactory;
            _sendObservable = sendObservable;
        }

        public async ValueTask Initialize(CancellationToken cancellationToken)
        {
            if (_producer != null)
            {
                throw new InvalidOperationException($"Producer with type {typeof(T).FullName} has already been initialized.");
            }

            _producer = await _producerFactory(cancellationToken);
        }

        public async Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken)
        {
            _sendObservable.PreSend(address, routingType, message);
            await _producer.SendAsync(address, routingType, message, transaction, cancellationToken).ConfigureAwait(false);
            _sendObservable.PostSend(address, routingType, message);
        }

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            _sendObservable.PreSend(address, routingType, message);
            _producer.Send(address, routingType, message, cancellationToken);
            _sendObservable.PostSend(address, routingType, message);
        }

        public async ValueTask DisposeAsync()
        {
            if (_producer != null)
            {
                await _producer.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}