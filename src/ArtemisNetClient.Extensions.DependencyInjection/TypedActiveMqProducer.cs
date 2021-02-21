using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class TypedActiveMqProducer<T> : IProducer, IProducerInitializer
    {
        private IProducer _producer;
        private readonly Func<CancellationToken, Task<IProducer>> _producerFactory;
        private readonly ContextualSendObservable _sendObservable;

        public TypedActiveMqProducer(Func<CancellationToken, Task<IProducer>> producerFactory, ContextualSendObservable sendObservable)
        {
            _producerFactory = producerFactory;
            _sendObservable = sendObservable;
        }

        async ValueTask IProducerInitializer.Initialize(CancellationToken cancellationToken)
        {
            if (_producer != null)
            {
                throw new InvalidOperationException($"Producer with type {typeof(T).FullName} has already been initialized.");
            }

            _producer = await _producerFactory(cancellationToken);
        }

        public async Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken)
        {
            CheckState();
            _sendObservable.PreSend(message);
            await _producer.SendAsync(message, transaction, cancellationToken).ConfigureAwait(false);
            _sendObservable.PostSend(message);
        }

        public void Send(Message message, CancellationToken cancellationToken)
        {
            CheckState();
            _sendObservable.PreSend(message);
            _producer.Send(message, cancellationToken);
            _sendObservable.PostSend(message);
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