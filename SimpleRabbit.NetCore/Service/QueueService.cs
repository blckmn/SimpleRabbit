using System;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore
{
    public interface IQueueService
    {
        void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1);
        void Start(Subscriber subscriber, IMessageHandler handler);
        void Stop();
    }

    public class QueueService : BasicRabbitService, IQueueService
    {
        private readonly ILogger<QueueService> _logger;

        private readonly Timer _timer;
        public QueueService(IOptions<RabbitConfiguration> options, ILogger<QueueService> logger) : base(options)
        {
            _logger = logger;

            _timer = new Timer
            {
                AutoReset = false,
            };
            _timer.Elapsed += (sender, args) =>
            {
                Start();
                _timer.Stop();
            };
        }

        private const int MaxRetryInterval = 120;
        private void RestartIn(TimeSpan waitInterval)
        {
            _retryCount++;
            var interval = waitInterval.TotalSeconds * (_autoBackOff ? _retryCount : 1) % MaxRetryInterval;
            
            _timer.Interval = interval * 1000;
            _logger.LogInformation($" -> restarting connection in {interval} seconds ({_retryCount}).");
            _timer.Start();
        }

        private string _queue;
        private string _tag;
        private IMessageHandler _handler;
        private ushort _prefetch;
        private int _retryInterval;
        private bool _autoBackOff;
        private int _retryCount;

        public void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1)
        {
            _prefetch = prefetch;
            _queue = queue;
            _tag = tag;
            _handler = handler;
            Start();
        }

        public void Start(Subscriber subscriber, IMessageHandler handler)
        {
            _prefetch = subscriber.PrefetchCount ?? 1;
            _queue = subscriber.QueueName;
            _tag = subscriber.ConsumerTag;
            _retryInterval = subscriber.RetryInterval;
            _autoBackOff = subscriber.AutoBackOff;
            _handler = handler;
            Start();
        }

        private void Start()
        {
            Stop();
            if (_handler == null)
            {
                throw new ArgumentNullException(nameof(_handler), $"No handler provided for {_tag}");
            }
            try
            {
                var consumer = new EventingBasicConsumer(Channel);
                consumer.Received += ReceiveEvent;
                Channel.BasicQos(0, _prefetch, false);
                Channel.BasicConsume(_queue, false, _tag, consumer);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                RestartIn(TimeSpan.FromSeconds(_retryInterval));
            }
        }

        public void Stop()
        {
            ClearConnection();
        }

        private void ReceiveEvent(object sender, BasicDeliverEventArgs args)
        {
            var channel = (sender as EventingBasicConsumer)?.Model;
            if (channel == null) throw new ArgumentNullException(nameof(sender), "Model null in received consumer event.");

            try
            {
                if (_handler?.Process(args) ?? false)
                {
                    channel.BasicAck(args.DeliveryTag, false);
                }
                _retryCount = 0;
            }
            catch (Exception ex)
            {
                // error processing message
                _logger.LogError(ex, $"{ex.Message} -> {args.DeliveryTag}: {args.BasicProperties.MessageId}");
                channel.BasicCancel(_tag);
                RestartIn(TimeSpan.FromSeconds(_retryInterval));
            }
        }
    }
}
