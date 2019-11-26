using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Dispatcher.Service.Models;
using System;
using System.Threading;

namespace Subscriber.Service.Service
{
    public class MessageOrderedHandler : OrderedDispatcher<Wrapper>
    {
        public MessageOrderedHandler(ILogger<OrderedDispatcher<Wrapper>> logger) : base(logger)
        {
        }

        public override bool CanProcess(string tag)
        {
            return true;
        }

        protected override Wrapper Get(BasicMessage message)
        {
            return new Wrapper
            {
                Model = JsonConvert.DeserializeObject<KeyedMessage>(message.Body)
            };
        }

        protected override string GetKey(Wrapper model)
        {
            return model.Model.Key;
        }

        protected override bool ProcessMessage(Wrapper message)
        {

            if (message.Model.Message.Equals("exception"))
            {
                throw new Exception("Error");
            }

            Thread.Sleep(1000);
            Console.WriteLine($"{message.Model.Key}, {message.Model.Message}");
            return true;
        }
    }
}
