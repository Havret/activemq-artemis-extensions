using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalUtils;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqProducer : IProducer
    {
        private readonly AsyncValueLazy<IProducer> _producer;

        public ActiveMqProducer(Func<CancellationToken, Task<IProducer>> producerFactory)
        {
            _producer = new AsyncValueLazy<IProducer>(producerFactory);
        }

        public async ValueTask DisposeAsync()
        {
            var producer = await _producer.GetValueAsync(CancellationToken.None);
            await producer.DisposeAsync();
        }

        public async Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken = new CancellationToken())
        {
            var producer = await _producer.GetValueAsync(cancellationToken);
            await producer.SendAsync(message, transaction, cancellationToken);
        }

        public void Send(Message message, CancellationToken cancellationToken = new CancellationToken())
        {
            var producer = _producer.GetValueAsync(cancellationToken).GetAwaiter().GetResult();
            producer.Send(message, cancellationToken);
        }
    }
}