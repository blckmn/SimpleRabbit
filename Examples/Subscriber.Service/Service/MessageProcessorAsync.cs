using SimpleRabbit.NetCore;
using System;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public class MessageProcessorAsync : IMessageHandlerAsync
    {
        public bool CanProcess(string tag)
        {
            return true;
        }

        public async Task<bool> Process(BasicMessage message)
        {
            Console.WriteLine(message.Body);

            if (message.Body.Equals("exception"))
            {
                throw new Exception("Error");
            }

            await Task.Delay(1000);


            // return true if you want to ack immediately. False if you want to handle the ack (e.g. dispatch to a thread - and ack later).
            return true;
        }
    }
}
