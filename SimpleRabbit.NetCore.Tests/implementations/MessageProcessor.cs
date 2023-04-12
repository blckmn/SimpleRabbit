using SimpleRabbit.NetCore;
using System;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : IMessageHandler
    {
        public Func<BasicMessage, Acknowledgement> Handler;
        public bool CanProcess(string tag)
        {
            return true;
        }

        public Acknowledgement Process(BasicMessage message)
        {
            return Handler.Invoke(message);
        }
    }
}
