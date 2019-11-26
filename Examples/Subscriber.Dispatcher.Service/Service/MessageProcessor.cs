using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Dispatcher.Service.Models;
using System;
using System.Threading;

namespace Subscriber.Service.Service
{
    public class MessageProcessor : OrderedQueueDispatcher<KeyedMessage>
    {
        public MessageProcessor(ILogger<OrderedQueueDispatcher<KeyedMessage>> logger) : base(logger)
        {
        }

        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override KeyedMessage Get(BasicMessage message)
        {
            return JsonConvert.DeserializeObject<KeyedMessage>(message.Body);
        }

        protected override string GetKey(KeyedMessage model)
        {
            return model.Key;
        }

        protected override bool ProcessMessage(KeyedMessage message)
        {

            if (message.Message.Equals("exception"))
            {
                throw new Exception("Error");
            }

            Thread.Sleep(1000);
            Console.WriteLine($"{message.Key}, {message.Message}");
            return true;
        }
    }
}
