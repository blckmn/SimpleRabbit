using System;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore
{
    public interface IQueueService : IBasicRabbitService
    {
        void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1);
        void Start(QueueConfiguration subscriberConfiguration, IMessageHandler handler);
        void Stop();
    }

    public class QueueService : BasicRabbitService, IQueueService
    {
        private const int MaxRetryInterval = 120;

        private readonly ILogger<QueueService> _logger;

        private readonly Timer _timer;
        private QueueConfiguration _queueServiceParams;

        private IMessageHandler _handler;
        private int _retryCount;

        public QueueService(RabbitConfiguration options, ILogger<QueueService> logger) : base(options)
        {
            _logger = logger;

            _timer = new Timer
            {
                AutoReset = false,
            };

            _timer.Elapsed += (sender, args) =>
            {
                _timer.Stop();
                Start();
            };
        }

        private void RestartIn(TimeSpan waitInterval)
        {
            try
            {
                // attempt to stop the event consumption.
                Channel?.BasicCancel(_queueServiceParams.ConsumerTag);
            }
            catch
            {
                // ignored
            }

            try
            {
                Close();
            }
            catch
            {
                // Ignored
            }

            _retryCount++;
            var interval = waitInterval.TotalSeconds * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MaxRetryInterval;

            _timer.Interval = interval * 1000;
            _logger.LogInformation($" -> restarting connection in {interval} seconds ({_retryCount}).");
            _timer.Start();
        }

        public void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1)
        {
            _queueServiceParams = new QueueConfiguration
            {
                PrefetchCount = prefetch,
                QueueName = queue,
                ConsumerTag = tag,
                RetryInterval = 30
            };
            _handler = handler;

            Start();
        }

        public void Start(QueueConfiguration subscriberConfiguration, IMessageHandler handler)
        {
            _queueServiceParams = subscriberConfiguration;
            if (_queueServiceParams.RetryInterval == 0)
            {
                _queueServiceParams.RetryInterval = 30;
            }
            _handler = handler;

            Start();
        }

        private void Start()
        {
            Stop();
            if (_handler == null)
            {
                throw new ArgumentNullException(nameof(_handler), $"No handler provided for {_queueServiceParams.ConsumerTag} => {_queueServiceParams.QueueName}");
            }

            try
            {
                var consumer = new EventingBasicConsumer(Channel);
                consumer.Received += ReceiveEvent;

                Channel.BasicQos(0, _queueServiceParams.PrefetchCount ?? 1, false);
                Channel.BasicConsume(_queueServiceParams.QueueName, false, _queueServiceParams.ConsumerTag, consumer);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{_queueServiceParams.QueueName} -> {e.Message}");
                RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryInterval));
            }
        }

        public void Stop()
        {
            Close();
        }

        private void ReceiveEvent(object sender, BasicDeliverEventArgs args)
        {
            var channel = (sender as EventingBasicConsumer)?.Model;
            if (channel == null) throw new ArgumentNullException(nameof(sender), "Model null in received consumer event.");

            try
            {
                var message = new BasicMessage(args, channel, _queueServiceParams.QueueName,
                    () => RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryInterval)));

                if (_handler.Process(message))
                {
                    channel.BasicAck(args.DeliveryTag, false);
                }
                _retryCount = 0;
            }
            catch (Exception ex)
            {
                // error processing message
                _logger.LogError(ex, $"{ex.Message} -> {args.DeliveryTag}: {args.BasicProperties.MessageId}");
                RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryInterval));
            }
        }
    }
}
