using System;
using System.Collections.Generic;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class ActiveMqOptions
    {
        public bool EnableQueueDeclaration { get; set; }
        public bool EnableAddressDeclaration { get; set; }
        public List<QueueConfiguration> QueueConfigurations { get; } = new List<QueueConfiguration>();
        public Dictionary<string, HashSet<RoutingType>> AddressConfigurations { get; set; } = new Dictionary<string, HashSet<RoutingType>>();
        public List<Action<IServiceProvider, ConnectionFactory>> ConnectionFactoryActions { get; } = new List<Action<IServiceProvider, ConnectionFactory>>();
    }
}