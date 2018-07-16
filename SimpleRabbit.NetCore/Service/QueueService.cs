using System;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore.Service
{
    public interface IQueueService
    {
        void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1);
        void Start(SubscriberConfiguration subscriberConfiguration, IMessageHandler handler);
        void Stop();
    }

    public class QueueService : BasicRabbitService, IQueueService
    {
        private const int MaxRetryInterval = 120;

        private readonly ILogger<QueueService> _logger;

        private readonly Timer _timer;
        private SubscriberConfiguration _queueServiceParams;

        private IMessageHandler _handler;
        private int _retryCount;

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

        private void RestartIn(TimeSpan waitInterval)
        {
            _retryCount++;
            var interval = waitInterval.TotalSeconds * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MaxRetryInterval;

            _timer.Interval = interval * 1000;
            _logger.LogInformation($" -> restarting connection in {interval} seconds ({_retryCount}).");
            _timer.Start();
        }

        public void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1)
        {
            _queueServiceParams = new SubscriberConfiguration
            {
                PrefetchCount = prefetch,
                QueueName = queue,
                ConsumerTag = tag
            };
            _handler = handler;

            Start();
        }

        public void Start(SubscriberConfiguration subscriberConfiguration, IMessageHandler handler)
        {
            _queueServiceParams = subscriberConfiguration;
            Start();
        }

        private void Start()
        {
            Stop();
            if (_handler == null)
            {
                throw new ArgumentNullException(nameof(_handler), $"No handler provided for {_queueServiceParams.ConsumerTag}");
            }
            try
            {
                var consumer = new EventingBasicConsumer(Channel);
                consumer.Received += ReceiveEvent;

                if (_handler is IChannelOptions options)
                {
                    options.SetChannelOptions(Channel,
                        () => RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryInterval)));
                }

                Channel.BasicQos(0, _queueServiceParams.PrefetchCount ?? 1, false);
                Channel.BasicConsume(_queueServiceParams.QueueName, false, _queueServiceParams.ConsumerTag, consumer);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryInterval));
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
                channel.BasicCancel(_queueServiceParams.ConsumerTag);
                RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryInterval));
            }
        }
    }
}
