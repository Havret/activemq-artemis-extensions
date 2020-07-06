using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class TypedActiveMqAnonymousProducer<T> : IAnonymousProducer, IProducerInitializer
    {
        private IAnonymousProducer _producer;
        private readonly Func<CancellationToken, Task<IAnonymousProducer>> _producerFactory;

        public TypedActiveMqAnonymousProducer(Func<CancellationToken, Task<IAnonymousProducer>> producerFactory)
        {
            _producerFactory = producerFactory;
        }

        public async ValueTask Initialize(CancellationToken cancellationToken)
        {
            if (_producer != null)
            {
                throw new InvalidOperationException($"Producer with type {typeof(T).FullName} has already been initialized.");
            }

            _producer = await _producerFactory(cancellationToken);
        }

        public Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken)
        {
            return _producer.SendAsync(address, routingType, message, transaction, cancellationToken);
        }

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            _producer.Send(address, routingType, message, cancellationToken);
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