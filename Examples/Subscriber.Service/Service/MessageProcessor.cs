using System;
using SimpleRabbit.NetCore;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : IMessageHandler
    {
        public bool CanProcess(string tag)
        {
            return true;
        }

        public bool Process(BasicMessage message)
        {
            Console.WriteLine(message.Body);

            // return true if you want to ack immediately. False if you want to handle the ack (e.g. dispatch to a thread - and ack later).
            return true;
        }
    }
}
