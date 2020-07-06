using System.Collections.Generic;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqOptions
    {
        public bool EnableQueueDeclaration { get; set; }
        public List<QueueConfiguration> QueueConfigurations { get; set; } = new List<QueueConfiguration>();
    }
}