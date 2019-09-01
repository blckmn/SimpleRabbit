using System;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore
{
    public interface IPublishService : IBasicRabbitService
    {
        void ToExchange(string exchange, string body, IBasicProperties properties = null, string route = "");
        void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null);
    }

    public class PublishService : BasicRabbitService, IPublishService
    {
        public int InactivityPeriod { get; set; }

        public PublishService(IOptionsSnapshot<RabbitConfiguration> options) : base(options)
        {
            InactivityPeriod = 30;
        }

        public void ToExchange(string exchange, string body, IBasicProperties properties = null, string route = "")
        {
            Publish(exchange, route, properties, body);
        }

        public void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null)
        {
            if (string.IsNullOrWhiteSpace(exchange) && string.IsNullOrWhiteSpace(route))
            {
                throw new Exception("Exchange (or route) must be provided.");
            }

            LastWatchDogTicks = DateTime.UtcNow.Ticks;
            
            lock (this)
            {
                Channel.ConfirmSelect();
                Channel.BasicPublish(exchange ?? "",
                    route ?? "",
                    properties,
                    Encoding.UTF8.GetBytes(body ?? ""));
                Channel.WaitForConfirmsOrDie();
            }
        }

        protected override void OnWatchdogExecution()
        {
            if (LastWatchDogTicks >= DateTime.UtcNow.AddSeconds(-InactivityPeriod).Ticks)
            {
                return;
            }
            LastWatchDogTicks = DateTime.UtcNow.Ticks;
            Close();
        }
    }
}
