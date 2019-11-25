namespace SimpleRabbit.NetCore
{
    public class QueueConfiguration
    {
        public string ExchangeName { get; set; }
        /// <summary>
        /// Name that will be displayed on RabbitMQ
        /// </summary>
        public string ConsumerTag { get; set; }
        /// <summary>
        /// Name to use to determine <see cref="IMessageHandler"/>
        /// If not configured, will be replaced with ConsumerTag
        /// </summary>
        public string HandlerTag { get; set; }
        public string QueueName { get; set; }
        public ushort? PrefetchCount { get; set; }
        /// <summary>
        /// On error, how long it waits until reattempting to restart consuming
        /// </summary>
        public int? RetryIntervalInSeconds { get; set; }
        public bool AutoBackOff { get; set; }
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
            DropMessage = 2
        }
    }

}
