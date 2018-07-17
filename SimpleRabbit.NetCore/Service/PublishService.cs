using System;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore
{
    public interface IPublishService
    {
        void ToExchange(string exchange, string body, IBasicProperties properties = null, string route = "");
        void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null);
    }

    public class PublishService : BasicRabbitService, IPublishService
    {

        public PublishService(IOptions<RabbitConfiguration> options) : base(options)
        { 

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

            Channel.BasicPublish(exchange ?? "",
                route ?? "",
                properties,
                Encoding.UTF8.GetBytes(body ?? ""));
        }
    }

}
