using System;
using SimpleRabbit.NetCore;

namespace Subscriber.Service.Service
{
    public class MessageProcessorDispatch : IMessageHandler, IDispatchHandler
    {
        public bool CanProcess(string tag)
        {
            if (tag == null) return false;

            return tag.ToUpper().EndsWith("DISPATCHER");
        }

        public bool Process(BasicMessage message)
        {
            Console.WriteLine(message.Body);
            return true;
        }

        public string GetKey(BasicMessage message)
        {
            return message.MessageId;
        }
    }
}
