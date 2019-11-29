using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Timers;

namespace SimpleRabbit.NetCore
{
    public interface IPublishService : IBasicRabbitService
    {
        void ToExchange(string exchange, string body, IBasicProperties properties = null, string route = "");
        void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null);
    }

    public class PublishService : BasicRabbitService, IPublishService
    {
        private const int DefaultInactivityTime = 30;
        private readonly int _inactivityPeriod;

        public PublishService(ILogger<PublishService> logger, RabbitConfiguration options) : base(options)
        {
            _inactivityPeriod = options.InactivityPeriodInSeconds ?? DefaultInactivityTime;

            _watchdogTimer = new Timer
            {
                AutoReset = true,
                Interval = _inactivityPeriod * 1000, // in seconds
                Enabled = false
            };

            _watchdogTimer.Elapsed += (sender, args) => { WatchdogExecution(); };

            LastWatchDogTicks = DateTime.UtcNow.Ticks;
            _logger = logger;
        }

        public void ToExchange(string exchange, string body, IBasicProperties properties = null, string route = "")
        {
            Publish(exchange, route, properties, body);
        }

        public void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null)
        {
            if (!_watchdogTimer.Enabled)
            {
                _watchdogTimer.Start();
            }

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

        /// <summary>
        /// A Timer to check for an idle connection, to ensure a connection is not held open indefinitely.
        /// </summary>
        /// <remarks> 
        /// Threading in the Connection prevent Console Applications from stopping if the connection
        /// is not closed (i.e inside a using clause or not calling close).
        /// </remarks>
        private readonly Timer _watchdogTimer;
        private readonly ILogger<PublishService> _logger;
        protected long LastWatchDogTicks = DateTime.UtcNow.Ticks;

        private void WatchdogExecution()
        {
            var acquired = false;
            try
            {
                _logger.LogTrace("Checking Watchdog timer");
                System.Threading.Monitor.TryEnter(this, ref acquired);
                if (!acquired)
                {
                    return;
                }

                OnWatchdogExecution();
            }
            finally
            {
                if (acquired)
                {
                    System.Threading.Monitor.Exit(this);
                }
            }
        }

        protected virtual void OnWatchdogExecution()
        {
            if (LastWatchDogTicks >= DateTime.UtcNow.AddSeconds(-_inactivityPeriod).Ticks)
            {
                return;
            }
            _logger.LogInformation($"Idle Rabbit Publishing Connection Detected, Clearing connection");
            ClearConnection();
            _watchdogTimer.Stop();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            _watchdogTimer?.Dispose();
        }
    }
}
