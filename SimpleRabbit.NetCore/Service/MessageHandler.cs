using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore
{
    public interface IMessageHandler
    {
        bool CanProcess(string tag);
        bool Process(BasicMessage message);
    }

    public class BasicMessage
    {
        public BasicMessage(BasicDeliverEventArgs deliveryArgs, IModel channel, string queue, Action registerError)
        {
            DeliveryArgs = deliveryArgs;
            Channel = channel;
            Queue = queue;
            RegisterError = registerError;
        }

        public BasicDeliverEventArgs DeliveryArgs { get; }
        public IModel Channel { get; }
        public string Queue { get; }
        public Action RegisterError { get; }

        public string Body => Encoding.UTF8.GetString(DeliveryArgs?.Body);
        public IBasicProperties Properties => DeliveryArgs?.BasicProperties;
        public ulong DeliveryTag => DeliveryArgs.DeliveryTag;
    }
}
