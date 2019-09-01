using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Options;
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

                var config = _config.Get(ConfigurationName);
                _hostnames = config.Hostnames ?? (string.IsNullOrWhiteSpace(config.Uri?.Host) ? new List<string>() : new List<string> { config.Uri?.Host });

                _factory = new ConnectionFactory
                {
                    Uri = config.Uri,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    TopologyRecoveryEnabled = true,
                    RequestedHeartbeat = 5
                };

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

        private string ClientName { get; }
        public string ConfigurationName { get; set; }
        private readonly IOptionsSnapshot<RabbitConfiguration> _config;

        protected BasicRabbitService(IOptionsSnapshot<RabbitConfiguration> options)
        {
            _config = options;
            ConfigurationName = Options.DefaultName;

            var config = options.Value;

            if (config?.Uri == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration not set for RabbitMQ");    
            }


            ClientName = config.Name ?? Environment.GetEnvironmentVariable("COMPUTERNAME") ?? Environment.GetEnvironmentVariable("HOSTNAME");

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
                try
                {
                    try
                    {
                        _timer.Change(Infinite, Infinite);
                        _channel?.Dispose();
                    }
                    finally
                    {
                        _connection?.Dispose();
                    }
                }
                finally
                {
                    _channel = null;
                    _connection = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Close();
            _timer?.Change(Infinite, Infinite);
            _timer?.Dispose();
        }

        ~BasicRabbitService()
        {
            Dispose(false);
        }
    }
}
