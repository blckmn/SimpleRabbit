using SimpleRabbit.NetCore;
using System;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : IMessageHandler
    {
        public Func<BasicMessage, bool> Handler;
        public bool CanProcess(string tag)
        {
            return true;
        }

        public bool Process(BasicMessage message)
        {
            return Handler.Invoke(message);
        }
    }
}
