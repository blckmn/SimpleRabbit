using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleRabbit.NetCore
{
    public class BasicMessage
    {
        public BasicMessage(BasicDeliverEventArgs deliveryArgs, IModel channel, string queue, Action errorAction)
        {
            DeliveryArgs = deliveryArgs;
            Channel = channel;
            Queue = queue;
            ErrorAction = errorAction;
        }

        public BasicDeliverEventArgs DeliveryArgs { get; }
        public IModel Channel { get; }
        public string Queue { get; }
        /// <summary>
        /// Action that should be called on exception in processing a message
        /// </summary>
        public Action ErrorAction { get; }

        /// <summary>
        /// The body of the message as a string
        /// </summary>
        /// <remarks> The body is natively in <see cref="byte[]"/></remarks>
        public string Body => Encoding.UTF8.GetString(DeliveryArgs?.Body);
        /// <summary>
        /// Process a body as raw bytes
        /// </summary>
        public byte[] BodyAsBytes => DeliveryArgs?.Body;
        public IBasicProperties Properties => DeliveryArgs?.BasicProperties;
        public ulong DeliveryTag => DeliveryArgs?.DeliveryTag ?? 0;
        public string ConsumerTag => DeliveryArgs?.ConsumerTag;
        public string MessageId => Properties?.MessageId;
        public IDictionary<string, object> Headers => Properties?.Headers;

        public void Ack()
        {
            Channel?.BasicAck(DeliveryTag, false);
        }

        public void Nack(bool requeue = true)
        {
            Channel?.BasicNack(DeliveryTag, false, requeue);
        }
    }
}
