using System.Collections.Generic;

namespace SimpleRabbit.NetCore
{
    public class SubscriberConfiguration : RabbitConfiguration
    {
        public List<QueueConfiguration> Subscribers { get;set;}

    }

    /// <summary>
    /// Empty class, to keep a record of listed subscribers.
    /// </summary>
    public class Subscribers
    {
        public string Name { get; set; }

    }
}
