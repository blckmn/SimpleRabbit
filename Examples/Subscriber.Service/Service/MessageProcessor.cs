using SimpleRabbit.NetCore;
using System;
using System.Threading;

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


            if (message.Body.Equals("exception"))
            {
                throw new Exception("Error");
            }

            Thread.Sleep(1000);
            Console.WriteLine(message.Body);
            // return true if you want to ack immediately. False if you want to handle the ack (e.g. dispatch to a thread - and ack later).
            return true;
        }
    }
}
