using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
        private readonly ILogger _baseLogger;

        protected BasicRabbitService(RabbitConfiguration config)
        {
            _config = config;
        }

        protected BasicRabbitService(RabbitConfiguration config, ILogger logger) : this(config)
        {
            _baseLogger = logger;
        }

        private IConnection _connection;
        /// <summary>
        /// ClientName is used only for human reference from RabbitMQ UI.
        /// </summary>
        protected IConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = Factory.CreateConnection(_hostnames, ClientName);

                    if (_connection is IAutorecoveringConnection autorecovering)
                    {
                        autorecovering.RecoverySucceeded += OnConnectionRecoverySucceeded;
                        autorecovering.ConnectionRecoveryError += OnConnectionRecoveryError;
                    }

                    _connection.ConnectionShutdown += OnConnectionShutdown;
                }

                return _connection;
            }
        }

        private IModel _channel;
        protected IModel Channel => _channel ?? (_channel = Connection.CreateModel());

        private void OnConnectionRecoverySucceeded(object sender, EventArgs e)
        {
            _baseLogger?.LogInformation("RabbitMQ connection recovered successfully, invalidating channel");
            lock (_lock)
            {
                // Invalidate the stale channel so it gets recreated on next access.
                // The connection itself is still valid (it just recovered).
                try
                {
                    _channel?.Dispose();
                }
                catch
                {
                    // Channel may already be disposed after recovery
                }
                _channel = null;
            }
            OnRecovered();
        }

        private void OnConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs e)
        {
            _baseLogger?.LogError(e.Exception, "RabbitMQ connection recovery failed");
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _baseLogger?.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
        }

        /// <summary>
        /// Called when the connection has been automatically recovered.
        /// Override to re-register consumers or perform other post-recovery actions.
        /// </summary>
        protected virtual void OnRecovered() { }

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
                    try
                    {
                        if (_connection != null)
                        {
                            if (_connection is IAutorecoveringConnection autorecovering)
                            {
                                autorecovering.RecoverySucceeded -= OnConnectionRecoverySucceeded;
                                autorecovering.ConnectionRecoveryError -= OnConnectionRecoveryError;
                            }
                            _connection.ConnectionShutdown -= OnConnectionShutdown;
                        }
                    }
                    finally
                    {
                        _connection?.Close();
                        _connection?.Dispose();
                        _connection = null;
                    }
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
