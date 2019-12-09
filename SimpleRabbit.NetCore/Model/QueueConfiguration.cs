namespace SimpleRabbit.NetCore
{
    public class QueueConfiguration
    {
        public string ExchangeName { get; set; }
        public string ConsumerTag { get; set; }
        public string QueueName { get; set; }
        public ushort? PrefetchCount { get; set; }
        public int RetryInterval { get; set; }
        public bool AutoBackOff { get; set; }
        public int HeartBeat { get; set; }
    }
}
