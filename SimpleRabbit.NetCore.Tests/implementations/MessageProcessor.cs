using RabbitMQ.Client;
using SimpleRabbit.NetCore;
using System;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : IMessageHandler
    {
        public event EventHandler<BasicMessage> Handler;
        public bool CanProcess(string tag)
        {
            return true;
        }

        public bool Process(BasicMessage message)
        {
            Handler(this, message);
            
            return true;
        }
    }
}
