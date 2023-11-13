using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore
{
    public interface IBasicRabbitService : IDisposable
    {
        IBasicProperties GetBasicProperties();
        void Close();
    }

    public abstract class BasicRabbitService : IBasicRabbitService
    {
        private readonly object _lock = new object();
        private const ushort DefaultRequestedHeartBeat = 5;
        private const int DefaultNetworkRecoveryInterval = 10;

        private IList<string> _hostnames;
        private ConnectionFactory _factory;
        protected ConnectionFactory Factory
        {
            get
            {
                if (_factory != null)
                {
                    return _factory;
                }

                if (string.IsNullOrWhiteSpace(_config.Username) || string.IsNullOrWhiteSpace(_config.Password))
                {
                    throw new InvalidCredentialException("Rabbit Configuration: Username or Password is missing");
                }

                if (_config.Hostnames == null || !_config.Hostnames.Any())
                {
                    throw new ArgumentNullException(nameof(_config.Hostnames), "Rabbit Configuration: No Hostnames provided");
                }

                _hostnames = _config.Hostnames;

                _factory = new ConnectionFactory
                {
                    UserName = _config.Username,
                    Password = _config.Password,
                    VirtualHost = string.IsNullOrWhiteSpace(_config.VirtualHost) ? ConnectionFactory.DefaultVHost : _config.VirtualHost,
                    AutomaticRecoveryEnabled = _config.AutomaticRecoveryEnabled ?? true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(_config.NetworkRecoveryIntervalInSeconds ?? DefaultNetworkRecoveryInterval),
                    TopologyRecoveryEnabled = _config.TopologyRecoveryEnabled ?? true,
                    RequestedHeartbeat = TimeSpan.FromSeconds(_config.RequestedHeartBeat ?? DefaultRequestedHeartBeat),
                    DispatchConsumersAsync = _config.UseAsyncDispatch ?? true,
                };

                return _factory;
            }
        }
        private string ClientName =>
            _config?.Name ?? 
            Environment.GetEnvironmentVariable("COMPUTERNAME") ??
            Environment.GetEnvironmentVariable("HOSTNAME");

        private readonly RabbitConfiguration _config;

        protected BasicRabbitService(RabbitConfiguration config)
        {
            _config = config;
        }

        private IConnection _connection;
        /// <summary>
        /// ClientName is used only for human reference from RabbitMQ UI.
        /// </summary>
        protected IConnection Connection => _connection ?? (_connection = Factory.CreateConnection(_hostnames, ClientName));

        private IModel _channel;
        protected IModel Channel => _channel ?? (_channel = Connection.CreateModel());

        public IBasicProperties GetBasicProperties()
        {
            lock (_lock)
            {
                return Channel.CreateBasicProperties();
            }
        }

        public void ClearConnection()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                try
                {
                    _channel?.Close();
                    _channel?.Dispose();
                    _channel = null;
                }
                finally
                {
                    _connection?.Close();
                    _connection?.Dispose();
                    _connection = null;
                }

            }
        }

        public void Close()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    ClearConnection();
                }
                finally
                {
                    _factory = null;
                    _channel = null;
                    _connection = null;
                }
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Any additional things to clean up.
        /// </summary>
        protected virtual void Cleanup() { }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            try
            {
                Cleanup();
                Close();
            }
            finally
            {
                _disposed = true;
            }
        }

        ~BasicRabbitService()
        {
            Dispose(false);
        }
    }
}
