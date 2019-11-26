using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Dispatcher.Service.Models;
using System;
using System.Text;
using System.Threading;

namespace Subscriber.Service.Service
{
    public class MessageUnorderedProcessor : UnorderedDispatcher
    {
        public MessageUnorderedProcessor(ILogger<MessageUnorderedProcessor> logger) : base(logger)
        {
        }

        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override bool ProcessMessage(BasicMessage message)
        {

            if (message.Body.Equals("exception"))
            {
                throw new Exception("Error");
            }

            Thread.Sleep(1000);
            Console.WriteLine($"{message.Body}");
            return true;
        }
    }
}
