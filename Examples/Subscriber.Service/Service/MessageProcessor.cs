using System;
using SimpleRabbit.NetCore;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : IMessageHandler
    {
        public bool CanProcess(string tag)
        {
            return tag?.ToUpper().EndsWith("STD") ?? false;
        }

        public bool Process(BasicMessage message)
        {
            Console.WriteLine(message.Body);
            return true;
        }
    }
}
