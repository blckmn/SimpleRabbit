using System;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
        private const int DefaultRetryInterval = 15;

        private readonly ILogger<QueueService> _logger;

        private readonly Timer _timer;
        private QueueConfiguration _queueServiceParams;
        private IMessageHandler _handler;
        private TimeSpan RetryInterval => TimeSpan.FromSeconds(_queueServiceParams?.RetryIntervalInSeconds ?? DefaultRetryInterval);
        
        private int _retryCount;

        private ConcurrentBag<ulong> _toBeNackedMessages = new ConcurrentBag<ulong>();

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
                TimerActivation();
            };
        }

        public void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1)
        {
            _queueServiceParams = new QueueConfiguration
            {
                PrefetchCount = prefetch,
                QueueName = queue,
                ConsumerTag = tag,
            };
            _handler = handler;

            Start();
        }

        public void Start(QueueConfiguration subscriberConfiguration, IMessageHandler handler)
        {
            _queueServiceParams = subscriberConfiguration;
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
                var asynccon = new AsyncEventingBasicConsumer(Channel);
                asynccon.Received += ReceiveEventAsync;
                consumer.Received += ReceiveEvent;

                Channel.BasicQos(0, _queueServiceParams.PrefetchCount ?? 1, false);
                Channel.BasicConsume(_queueServiceParams.QueueName, false, _queueServiceParams.DisplayName ?? _queueServiceParams.ConsumerTag, consumer);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{_queueServiceParams.QueueName} -> {e.Message}");
                RestartIn(RetryInterval);
            }
        }

        private async Task ReceiveEventAsync(object sender, BasicDeliverEventArgs args)
        {
            
        }

        private void ReceiveEvent(object sender, BasicDeliverEventArgs args)
        {
            var channel = (sender as EventingBasicConsumer)?.Model;
            if (channel == null) throw new ArgumentNullException(nameof(sender), "Model null in received consumer event.");
            var message = new BasicMessage(args, channel, _queueServiceParams.QueueName, () => OnError(sender, args));
            try
            {
                var acknowledgement = _handler.Process(message);
                message.HandleAck(acknowledgement);
                _retryCount = 0;
            }
            catch (Exception ex)
            {
                // error processing message
                _logger.LogError(ex, $"{ex.Message} -> {args.DeliveryTag}: {args.BasicProperties.MessageId}");
                message.ErrorAction?.Invoke();
            }
        }

        private void OnError(object sender, BasicDeliverEventArgs message)
        {
            try
            {
                var channel = (sender as IBasicConsumer)?.Model;
                if (channel == null)
                {
                    _logger.LogWarning($"Channel null, in OnError -> {_queueServiceParams.QueueName}");
                    RestartIn(RetryInterval);
                    return;
                }

                switch (_queueServiceParams.OnErrorAction)
                {
                    case QueueConfiguration.ErrorAction.DropAfterOneRedelivery:
                    {
                        if (message.Redelivered)
                        {
                            goto case QueueConfiguration.ErrorAction.DropMessage;
                        }
                        channel.BasicNack(message.DeliveryTag, false, true);
                        _logger.LogInformation($" -> nacked message on queue {_queueServiceParams.QueueName}");
                        return;
                    }
                    case QueueConfiguration.ErrorAction.DropMessage:
                    {
                        channel.BasicNack(message.DeliveryTag, false, false);
                        _logger.LogInformation($" -> dropped message on queue {_queueServiceParams.QueueName}");
                        return;
                    }
                    case QueueConfiguration.ErrorAction.NackOnException:
                    {
                        if (channel.IsClosed)
                        {
                            RestartIn(RetryInterval);
                        }
                        else
                        {
                            //queue up message to be nacked on restart.
                            _toBeNackedMessages.Add(message.DeliveryTag);
                            RestartIn(RetryInterval);
                        }
                        return;
                    }
                    case QueueConfiguration.ErrorAction.RestartConnection:
                    default:
                    {
                        RestartIn(RetryInterval);
                        channel.BasicNack(message.DeliveryTag, false, true);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occurred while trying to handle another error, restarting connection -> {e.Message}");
                RestartIn(RetryInterval);
            }
        }

        private void RestartIn(TimeSpan waitInterval)
        {
            if (_timer.Enabled)
            {
                // another message has already triggered an error.
                return;
                
            }

            if (_queueServiceParams.OnErrorAction == QueueConfiguration.ErrorAction.RestartConnection)
            {
                try
                {
                    //take note of blocking if clearing connection here
                    //https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/341
                    // attempt to stop the event consumption.
                    if (Channel != null && !Channel.IsClosed)
                        Channel?.BasicCancel(_queueServiceParams.ConsumerTag);
                }
                catch
                {
                    // ignored
                }
            }

            _retryCount++;
            var interval = waitInterval.TotalSeconds * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MaxRetryInterval;

            _timer.Interval = interval * 1000; // seconds
            _logger.LogInformation($" -> restarting in {interval} seconds ({_retryCount}).");
            _timer.Start();
        }

        private void TimerActivation()
        {
            switch (_queueServiceParams.OnErrorAction)
            {
                case QueueConfiguration.ErrorAction.NackOnException:
                {
                    foreach (var message in _toBeNackedMessages)
                    {
                        if (Channel != null && !Channel.IsClosed)
                        {
                            Channel.BasicNack(message, false, true);
                        }
                        
                    }
                    _toBeNackedMessages.Clear();
                    return;
                }
                case QueueConfiguration.ErrorAction.RestartConnection:
                default:
                {
                    Start();
                    return;
                }
            }
        }

        public void Stop()
        {
            Close();
        }
    }
}
