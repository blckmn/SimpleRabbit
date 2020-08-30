namespace SimpleRabbit.NetCore
{
    public class QueueConfiguration
    {
        public string ExchangeName { get; set; }
        public string ConsumerTag { get; set; }
        public string QueueName { get; set; }
        /// <summary>
        /// The name to display inside of Rabbit MQ. defauls to ConsumerTag if not specified
        /// </summary>
        public string DisplayName { get; set; }
        public ushort? PrefetchCount { get; set; }
        /// <summary>
        /// On error, how long it waits until reattempting to restart consuming
        /// </summary>
        public int? RetryIntervalInSeconds { get; set; }
        public bool AutoBackOff { get; set; }
        public int HeartBeat { get; set; }
        public ErrorAction OnErrorAction { get; set; }

        public enum ErrorAction
        {
            /// <summary>
            /// The connection will be completed cleared, and restarted in <see cref="RetryIntervalInSeconds"/>
            /// </summary>
            RestartConnection = 0,
            /// <summary>
            /// Messages will be requeued and consuming will start after the <see cref="RetryIntervalInSeconds"/>
            /// </summary>
            NackOnException = 1,
            /// <summary>
            /// Errorred messages will be Nacked and not requeued
            /// </summary>
            DropMessage = 2,
            /// <summary>
            /// Errored Messages will be Nacked and not requeued after the first time.
            /// </summary>
            /// <remarks>
            /// There is a limit of one redelivery, as the internal flag is a boolean
            /// https://github.com/rabbitmq/rabbitmq-server/issues/502
            /// </remarks>
            DropAfterOneRedelivery = 3
        }
    }
}
