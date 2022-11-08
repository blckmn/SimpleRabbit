using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore
{
    public interface IPublishService : IBasicRabbitService
    {
        void ToExchange(string exchange, string body, IBasicProperties properties = null, string route = "", TimeSpan? timeout = null);
        void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null, TimeSpan? timeout = null);
    }

    public class PublishService : BasicRabbitService, IPublishService
    {
        /// <summary>
        /// A Timer to check for an idle connection, to ensure a connection is not held open indefinitely.
        /// </summary>
        /// <remarks> 
        /// Threading in the Connection prevent Console Applications from stopping if the connection
        /// is not closed (i.e inside a using clause or not calling close).
        /// </remarks>
        private readonly Timer _watchdogTimer;
        private readonly ILogger<PublishService> _logger;
        protected long lastWatchDogTicks;


        public int InactivityPeriod { get; set; }

        public PublishService(ILogger<PublishService> logger, RabbitConfiguration options) : base(options)
        {
            InactivityPeriod = 30;

            _watchdogTimer = new Timer
            {
                AutoReset = true,
                Interval = InactivityPeriod * 1000, // in seconds
                Enabled = false
            };

            _watchdogTimer.Elapsed += (sender, args) => { WatchdogExecution(); };

            lastWatchDogTicks = DateTime.UtcNow.Ticks;
            _logger = logger;
        }

        public void ToExchange(string exchange, string body, IBasicProperties properties = null, string route = "", TimeSpan? timeout = null)
        {
            Publish(exchange, route, properties, body, timeout);
        }

        public void Publish(string exchange = "", string route = "", IBasicProperties properties = null, string body = null, TimeSpan? timeout = null)
        {
            if (!_watchdogTimer.Enabled)
            {
                _watchdogTimer.Start();
            }

            if (string.IsNullOrWhiteSpace(exchange) && string.IsNullOrWhiteSpace(route))
            {
                throw new Exception("Exchange (or route) must be provided.");
            }

            lastWatchDogTicks = DateTime.UtcNow.Ticks;
            
            if (properties == null) 
            {
                properties = GetBasicProperties();
                properties.Headers ??= new Dictionary<string, object>();

                properties.Timestamp = new AmqpTimestamp((int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);
                properties.Headers.Add("_enqueueTime", DateTime.UtcNow.ToString("O"));
            }
            properties.MessageId ??= Guid.NewGuid().ToString("D");

            lock (this)
            {                
                Channel.ConfirmSelect();
                Channel.BasicPublish(exchange ?? "",
                    route ?? "",
                    properties,
                    Encoding.UTF8.GetBytes(body ?? ""));
                if (timeout != null)
                    Channel.WaitForConfirmsOrDie(timeout.Value);
                else
                    Channel.WaitForConfirmsOrDie();
            }
        }

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

        protected void OnWatchdogExecution()
        {
            if (lastWatchDogTicks >= DateTime.UtcNow.AddSeconds(-InactivityPeriod).Ticks)
            {
                return;
            }
            _logger.LogInformation($"Idle Rabbit Publishing Connection Detected, Clearing connection");
            lastWatchDogTicks = DateTime.UtcNow.Ticks;
            Close();
            _watchdogTimer.Stop();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            _watchdogTimer?.Dispose();
        }
    }
}
