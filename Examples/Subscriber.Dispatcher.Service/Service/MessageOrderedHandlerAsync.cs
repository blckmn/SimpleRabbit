using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Dispatcher.Service.Models;
using System;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public class MessageOrderedHandlerAsync : OrderedDispatcherAsync<Wrapper>
    {
        public MessageOrderedHandlerAsync(ILogger<OrderedDispatcherAsync<Wrapper>> logger) : base(logger)
        {
        }

        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override Task<Wrapper> Get(BasicMessage message)
        {
            return Task.FromResult( new Wrapper
            {
                Model = JsonConvert.DeserializeObject<KeyedMessage>(message.Body)
            });
           
        }

        protected override string GetKey(Wrapper model)
        {
            return model.Model.Key;
        }

        protected override async Task<bool> ProcessMessage(Wrapper message)
        {

            if (message.Model.Message.Equals("exception"))
            {
                throw new Exception("Error");
            }

            await Task.Delay(1000);
            Console.WriteLine($"{message.Model.Key}, {message.Model.Message}");
            return true;
        }
    }
}
