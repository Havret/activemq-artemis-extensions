using System;
using System.Collections.Generic;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    internal class TopologyRegistry
    {
        public Dictionary<string, List<QueueConfiguration>> NamedQueueConfigurations { get;  } = new Dictionary<string, List<QueueConfiguration>>(StringComparer.InvariantCultureIgnoreCase);
    }
}