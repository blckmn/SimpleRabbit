using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using System;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public class MessageUnorderedHandlerAsync : DispatcherAsync
    {
        public MessageUnorderedHandlerAsync(ILogger<MessageUnorderedHandlerAsync> logger) : base(logger)
        {
        }

        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override async Task<bool> ProcessMessage(BasicMessage message)
        {

            if (message.Body.Equals("exception"))
            {
                throw new Exception("Error");
            }

            await Task.Delay(1000);
            Console.WriteLine($"{message.Body}");
            return true;
        }
    }
}
