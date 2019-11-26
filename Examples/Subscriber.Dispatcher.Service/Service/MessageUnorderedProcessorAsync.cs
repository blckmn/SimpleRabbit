using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Dispatcher.Service.Models;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public class MessageUnorderedProcessorAsync : UnorderedDispatcherAsync
    {
        public MessageUnorderedProcessorAsync(ILogger<MessageUnorderedProcessorAsync> logger) : base(logger)
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
