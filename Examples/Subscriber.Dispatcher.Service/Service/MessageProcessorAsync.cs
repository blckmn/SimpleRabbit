using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Dispatcher.Service.Models;
using System;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public class MessageProcessorAsync : OrderedQueueDispatcherAsync<KeyedMessage>
    {
        public MessageProcessorAsync(ILogger<OrderedQueueDispatcherAsync<KeyedMessage>> logger) : base(logger)
        {
        }

        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override Task<KeyedMessage> Get(BasicMessage message)
        {
            return Task.FromResult(JsonConvert.DeserializeObject<KeyedMessage>(message.Body));
        }

        protected override string GetKey(KeyedMessage model)
        {
            return model.Key;
        }

        protected override async Task<bool> ProcessMessage(KeyedMessage message)
        {

            if (message.Message.Equals("exception"))
            {
                throw new Exception("Error");
            }

            await Task.Delay(1000);
            Console.WriteLine($"{message.Key}, {message.Message}");
            return true;
        }
    }
}
