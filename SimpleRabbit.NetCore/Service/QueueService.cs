using System;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Threading;
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
        private readonly object _restartLock = new object();

        private readonly Timer _timer;
        private QueueConfiguration _queueServiceParams;
        private IMessageHandler _handler;
        private TimeSpan RetryInterval => TimeSpan.FromSeconds(_queueServiceParams?.RetryIntervalInSeconds ?? DefaultRetryInterval);

        private int _retryCount;
        private int _restarting;
        private volatile bool _stopping;

        private ConcurrentBag<ulong> _toBeNackedMessages = new ConcurrentBag<ulong>();

        public QueueService(RabbitConfiguration options, ILogger<QueueService> logger) : base(options, logger)
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
            _stopping = false;
            if (_handler == null)
            {
                throw new ArgumentNullException(nameof(_handler), $"No handler provided for {_queueServiceParams.ConsumerTag} => {_queueServiceParams.QueueName}");
            }

            try
            {
                var consumer = new AsyncEventingBasicConsumer(Channel);
                consumer.Received += ReceiveEventAsync;
                consumer.ConsumerCancelled += OnConsumerCancelled;
                consumer.Shutdown += OnConsumerShutdown;

                Channel.BasicQos(0, _queueServiceParams.PrefetchCount ?? 1, false);
                Channel.BasicConsume(_queueServiceParams.QueueName, false, _queueServiceParams.DisplayName ?? _queueServiceParams.ConsumerTag, consumer);

                _logger.LogInformation($"Consumer started on queue {_queueServiceParams.QueueName}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{_queueServiceParams.QueueName} -> {e.Message}");
                RestartIn(RetryInterval);
            }
        }

        private Task OnConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            if (_stopping) return Task.CompletedTask;
            _logger.LogWarning($"Consumer cancelled by broker on queue {_queueServiceParams.QueueName}, tags: {string.Join(", ", e.ConsumerTags)}. Scheduling restart.");
            RestartIn(RetryInterval);
            return Task.CompletedTask;
        }

        private Task OnConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            if (_stopping) return Task.CompletedTask;
            _logger.LogWarning($"Consumer shutdown on queue {_queueServiceParams.QueueName}: {e.ReplyText}. Scheduling restart.");
            RestartIn(RetryInterval);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called by BasicRabbitService when the connection has been automatically recovered.
        /// Re-registers the consumer since the old channel/consumer is stale after recovery.
        /// </summary>
        protected override void OnRecovered()
        {
            if (_stopping) return;
            _logger.LogInformation($"Connection recovered, restarting consumer on queue {_queueServiceParams?.QueueName}");
            if (_queueServiceParams != null && _handler != null)
            {
                RestartIn(TimeSpan.FromSeconds(1));
            }
        }

        private async Task ReceiveEventAsync(object sender, BasicDeliverEventArgs args)
        {
            var channel = (sender as AsyncEventingBasicConsumer)?.Model;
            if (channel == null) throw new ArgumentNullException(nameof(sender), "Model null in received consumer event.");
            var message = new BasicMessage(args, channel, _queueServiceParams.QueueName, () => OnError(sender, args));
            try
            {
                var acknowledgement = await _handler.Process(message);
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
                        try
                        {
                            if (!channel.IsClosed)
                            {
                                channel.BasicNack(message.DeliveryTag, false, true);
                            }
                        }
                        catch (Exception nackEx)
                        {
                            _logger.LogWarning(nackEx, $"Failed to nack message {message.DeliveryTag} on queue {_queueServiceParams.QueueName} (channel may be closed)");
                        }
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
            // Use atomic compare-exchange to ensure only one thread enters the restart path.
            if (Interlocked.CompareExchange(ref _restarting, 1, 0) != 0)
            {
                // Another thread is already handling the restart.
                return;
            }

            try
            {
                if (_queueServiceParams.OnErrorAction == QueueConfiguration.ErrorAction.RestartConnection)
                {
                    try
                    {
                        //take note of blocking if clearing connection here
                        //https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/341
                        // attempt to stop the event consumption.
                        if (Channel != null && !Channel.IsClosed)
                            Channel?.BasicCancel(_queueServiceParams.DisplayName ?? _queueServiceParams.ConsumerTag);
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
            catch (Exception e)
            {
                _logger.LogError(e, $"Error scheduling restart for queue {_queueServiceParams.QueueName}");
                // Reset the flag so a future attempt can try again
                Interlocked.Exchange(ref _restarting, 0);
            }
        }

        private void TimerActivation()
        {
            // Reset the restart flag so future errors can schedule a new restart.
            Interlocked.Exchange(ref _restarting, 0);

            switch (_queueServiceParams.OnErrorAction)
            {
                case QueueConfiguration.ErrorAction.NackOnException:
                {
                    try
                    {
                        foreach (var message in _toBeNackedMessages)
                        {
                            if (Channel != null && !Channel.IsClosed)
                            {
                                Channel.BasicNack(message, false, true);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, $"Failed to nack queued messages on {_queueServiceParams.QueueName}, scheduling restart");
                        _toBeNackedMessages.Clear();
                        RestartIn(RetryInterval);
                        return;
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
            _stopping = true;
            Interlocked.Exchange(ref _restarting, 0);
            _timer.Stop();
            Close();
        }

        protected override void Cleanup()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
