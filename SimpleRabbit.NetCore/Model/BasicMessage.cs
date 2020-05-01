using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore
{
    public class BasicMessage
    {

        public BasicMessage(BasicDeliverEventArgs deliveryArgs, IModel channel, string queue, Action registerError)
        {
            DeliveryArgs = deliveryArgs;

            // client 6.0 - this needs to be read immediately, and stored as it is no longer thread safe.
            RawBody = DeliveryArgs?.Body.ToArray();
            Channel = channel;
            Queue = queue;
            ErrorAction = registerError;
        }

        public BasicDeliverEventArgs DeliveryArgs { get; }
        public IModel Channel { get; }
        public string Queue { get; }
        public Action ErrorAction { get; }

        public byte[] RawBody { get; }
        public string Body => Encoding.UTF8.GetString(RawBody);
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
