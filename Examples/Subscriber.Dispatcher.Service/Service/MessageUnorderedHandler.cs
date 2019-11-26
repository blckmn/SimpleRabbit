using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;
using System;
using System.Threading;

namespace Subscriber.Service.Service
{
    public class MessageUnorderedHandler : SimpleRabbit.NetCore.Service.Dispatcher
    {
        public MessageUnorderedHandler(ILogger<MessageUnorderedHandler> logger) : base(logger)
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
