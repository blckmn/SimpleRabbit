using System;
using System.Text;
using System.Threading;
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
        private const int Infinite = -1;
        private const int DefaultThreshold = 30000;

        public int InactivityPeriod { get; set; }
        /*
         Timer is a small hack that if someone publishes, the connection is not held open indefinitely.
         This is due to the threading in the Connection that prevents Console appications from stopping if
         connection is not closed (i.e. inside a using clause or not calling close). 
        */
        private readonly Timer _timer;
        public PublishService(IOptions<RabbitConfiguration> options) : base(options)
        {
            InactivityPeriod = DefaultThreshold;
            _timer = new Timer(state =>
                {
                    _timer.Change(Infinite, Infinite);
                    Close();
                }, this, Infinite, Infinite
            );
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

            _timer.Change(Infinite, InactivityPeriod);
            
            Channel.BasicPublish(exchange ?? "",
                route ?? "",
                properties,
                Encoding.UTF8.GetBytes(body ?? ""));
        }

        protected override void Dispose(bool disposing)
        {
            _timer.Change(Infinite, Infinite);
            base.Dispose(disposing);
        }
    }

}
