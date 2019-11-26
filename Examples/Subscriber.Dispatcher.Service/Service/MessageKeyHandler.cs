using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using System;
using System.Text;
using System.Threading;

namespace Subscriber.Service.Service
{
    public class MessageKeyHandler : OrderedKeyDispatcher
    {
        public MessageKeyHandler(ILogger<MessageKeyHandler> logger) : base(logger)
        {
        }

        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override string GetKey(BasicMessage model)
        {
            var o = model.Headers["key"] as byte[];

            return Encoding.UTF8.GetString(o);
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
