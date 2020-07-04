using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class TypedActiveMqProducer<T> : IProducer, IProducerInitializer
    {
        private IProducer _producer;
        private readonly Func<CancellationToken, Task<IProducer>> _producerFactory;

        public TypedActiveMqProducer(Func<CancellationToken, Task<IProducer>> producerFactory)
        {
            _producerFactory = producerFactory;
        }

        async ValueTask IProducerInitializer.Initialize(CancellationToken cancellationToken)
        {
            if (_producer != null)
            {
                throw new InvalidOperationException($"Producer with type {typeof(T).FullName} has already been initialized.");
            }

            _producer = await _producerFactory(cancellationToken);
        }

        public Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken)
        {
            CheckState();
            return _producer.SendAsync(message, transaction, cancellationToken);
        }

        public void Send(Message message, CancellationToken cancellationToken)
        {
            CheckState();
            _producer.Send(message, cancellationToken);
        }

        private void CheckState()
        {
            if (_producer == null)
            {
                throw new InvalidOperationException("Producer was not initialized.");
            }
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