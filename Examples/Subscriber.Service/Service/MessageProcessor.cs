using System;
using System.Threading.Tasks;
using SimpleRabbit.NetCore;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : IMessageHandler
    {
        public bool CanProcess(string tag)
        {
            return true;
        }

        public async Task<Acknowledgement> Process(BasicMessage message)
        {
            Console.WriteLine(message.Body);

            // See Acknowledgement enum for available options.
            return Acknowledgement.Ack;
        }
    }
}
