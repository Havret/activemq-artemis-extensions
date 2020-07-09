namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.InternalExtensions
{
    internal static class ProducerOptionsExtensions
    {
        public static ProducerConfiguration ToConfiguration(this ProducerOptions producerOptions)
        {
            return new ProducerConfiguration
            {
                MessagePriority = producerOptions.MessagePriority,
                MessageDurabilityMode = producerOptions.MessageDurabilityMode,
                MessageIdPolicy = producerOptions.MessageIdPolicy,
                SetMessageCreationTime = producerOptions.SetMessageCreationTime,
            };
        }
    }
}