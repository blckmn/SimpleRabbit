using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore
{
    public abstract class BasicRabbitService : IDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly IList<string> _hostnames;

        protected BasicRabbitService(IOptions<RabbitConfiguration> options)
        {
            var config = options.Value;

            _hostnames = config.Hostnames ?? 
                         (string.IsNullOrWhiteSpace(config.Uri?.Host) ? new List<string>() : new List<string> { config.Uri?.Host });
            _factory = new ConnectionFactory
            {
                Uri = config.Uri,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                TopologyRecoveryEnabled = true,
                RequestedHeartbeat = 5
            };
        }

        private IConnection _connection;
        protected IConnection Connection => _connection ?? (_connection = _factory.CreateConnection(_hostnames));

        private IModel _channel;
        protected IModel Channel => _channel ?? (_channel = Connection.CreateModel());

        public IBasicProperties GetBasicProperties()
        {
            return Channel.CreateBasicProperties();
        }

        protected void CloseConnection()
        {
            try 
            {
                _channel.Dispose();
                _channel = null;
            }
            finally 
            {
                _connection?.Dispose();
                _connection = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            CloseConnection();
        }

        ~BasicRabbitService()
        {
            Dispose(false);
        }
    }
}
