using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public class MessageKeyHandlerAsync : OrderedKeyDispatcherAsync
    {
        public MessageKeyHandlerAsync(ILogger<MessageKeyHandlerAsync> logger) : base(logger)
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
