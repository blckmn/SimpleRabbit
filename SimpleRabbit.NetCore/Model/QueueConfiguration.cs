using System.Collections.Generic;

namespace SimpleRabbit.NetCore
{
    public class QueueConfiguration
    {
        public string ExchangeName { get; set; }
        /// <summary>
        /// Name that will be displayed on RabbitMQ
        /// </summary>
        public string ConsumerName { get; set; }
        
        /// <summary>
        /// Name to use to determine <see cref="IMessageHandler"/>
        /// </summary>
        public string ConsumerTag { get; set; }
        public string QueueName { get; set; }
        public ushort? PrefetchCount { get; set; }
        /// <summary>
        /// On error, how long it waits until reattempting to restart consuming
        /// </summary>
        public int? RetryIntervalInSeconds { get; set; }
        public bool AutoBackOff { get; set; }
        /// <summary>
        /// Name of the queue to publish a message to.
        /// </summary>
        public string DeadLetterQueue { get;set;}
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
