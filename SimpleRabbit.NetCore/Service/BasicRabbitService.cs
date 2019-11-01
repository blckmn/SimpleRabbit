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
        private const int Infinite = -1;
        private const int TimerPeriod = 10000;

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
                /// factory instaniation logic.
                if (_config.Uri?.Host == null && (_config.Hostnames == null || !_config.Hostnames.Any()))
                {
                    throw new ArgumentException("Rabbit Configuration: No Uri or Hostnames provided");
                }

                if (string.IsNullOrWhiteSpace(_config.Uri?.UserInfo) && (string.IsNullOrWhiteSpace(_config.Username) || string.IsNullOrWhiteSpace(_config.Password)))
                {
                    throw new InvalidCredentialException("Username or password is missing");
                }

                _hostnames = _config.Hostnames ?? new List<string>();

                Uri uri = null;
                if (string.IsNullOrWhiteSpace(_config.Uri?.Host)) 
                {
                    // Randomize the first host chosen, such that load one particular cluster is not overloaded.
                    var i = new Random().Next(0, _hostnames.Count);
                    uri = new Uri($"amqp://{_config.Hostnames[i]}");
                }
                else
                {
                    _hostnames.Add(_config.Uri.Host);
                    uri = _config.Uri;
                }

                _factory = new ConnectionFactory
                {
                    Uri = uri,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    TopologyRecoveryEnabled = true,
                    RequestedHeartbeat = 5
                };

                // If user info is in uri, don't override them with username password.
                if (string.IsNullOrWhiteSpace(_config.Uri?.UserInfo))
                {
                    _factory.UserName = _config.Username;
                    _factory.Password = _config.Password;
                }

                return _factory;
            }
        }

        /*
         Timer is a small hack that if someone publishes, the connection is not held open indefinitely.
         This is due to the threading in the Connection that prevents Console applications from stopping if
         connection is not closed (i.e. inside a using clause or not calling close).
        */
        private readonly Timer _timer;
        protected long LastWatchDogTicks = DateTime.UtcNow.Ticks;
        protected abstract void OnWatchdogExecution();

        protected void WatchdogExecution()
        {
            var acquired = false;
            try
            {
                Monitor.TryEnter(this, ref acquired);
                if (!acquired)
                {
                    return;
                }

                OnWatchdogExecution();
            }
            finally
            {
                if (acquired) Monitor.Exit(this);
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

            _timer = new Timer(state => 
                {
                    WatchdogExecution();
                }, 
                this, 
                Infinite, 
                TimerPeriod
            );
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
            lock(this)
            {
                return Channel.CreateBasicProperties();
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    try
                    {
                        _timer?.Change(Infinite, Infinite);
                        _channel?.Dispose();
                    }
                    finally
                    {
                        _connection?.Dispose();
                    }
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            try
            {
                Close();
                _timer?.Dispose();
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
