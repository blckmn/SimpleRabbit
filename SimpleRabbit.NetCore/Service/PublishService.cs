using System;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore
{
    public interface IPublishService
    {
        void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null);
    }

    public class PublishService : IPublishService, IDisposable
    {
        private readonly ConnectionFactory _factory;

        public PublishService(IOptions<RabbitConfiguration> options)
        { 
            var config = options.Value;

            _factory = new ConnectionFactory
            {
                Uri = config.Uri,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                TopologyRecoveryEnabled = true
            };
        }

        private IConnection _connection;
        private IConnection Connection => _connection ?? (_connection = _factory.CreateConnection());

        private IModel _channel;
        private IModel Channel => _channel ?? (_channel = Connection.CreateModel());

        public IBasicProperties GetBasicProperties()
        {
            return Channel.CreateBasicProperties();
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

        public void Dispose()
        {
            _channel?.Dispose();
            _channel = null;

            _connection?.Dispose();
            _connection = null;
        }
    }

}
