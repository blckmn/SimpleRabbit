using System;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SimpleRabbit.NetCore.Model;

namespace SimpleRabbit.NetCore.Service
{
    public interface IPublishService
    {
        void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null);
    }

    public class PublishService : BasicRabbitService, IPublishService
    {

        public PublishService(IOptions<RabbitConfiguration> options) : base(options)
        { 

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
