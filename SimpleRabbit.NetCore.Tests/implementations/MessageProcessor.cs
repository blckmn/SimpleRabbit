using SimpleRabbit.NetCore;
using System;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : IMessageHandler
    {
        public Func<BasicMessage, Acknowledgement> Handler;
        public bool CanProcess(string tag)
        {
            return true;
        }

        public async Task<Acknowledgement> Process(BasicMessage message)
        {
            return Handler.Invoke(message);
        }
    }
}
