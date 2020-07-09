using ActiveMQ.Artemis.Client.MessageIdPolicy;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    public class ProducerOptions
    {
        public byte? MessagePriority { get; set; }
        public DurabilityMode? MessageDurabilityMode { get; set; }
        public bool SetMessageCreationTime { get; set; } = true;
        public IMessageIdPolicy MessageIdPolicy { get; set; }
    }
}