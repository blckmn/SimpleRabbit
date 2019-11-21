using System;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;

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
        private const int DefaultRetryInterval = 15;
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

            if (_queueServiceParams.OnErrorAction == QueueConfiguration.ErrorAction.DropMessage)
            {

                if (string.IsNullOrWhiteSpace(_queueServiceParams.DeadLetterQueue))
                {
                    throw new ArgumentNullException(nameof(_queueServiceParams.DeadLetterQueue), $"no dead letter queue assigned for {_queueServiceParams.ConsumerTag} => {_queueServiceParams.QueueName}");
                }
                
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
                RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryIntervalInSeconds ?? DefaultRetryInterval));
            }
        }

        private void RestartIn(TimeSpan waitInterval)
        {
            try
            {
                // attempt to stop the event consumption.
                Channel?.BasicCancel(_queueServiceParams.ConsumerTag);
            }
            catch { }// ignored

            try
            {
                Close();
            }
            catch { } //Ignored

            _retryCount++;
            var interval = waitInterval.TotalSeconds * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MaxRetryInterval;

            _timer.Interval = interval * 1000; // seconds
            _logger.LogInformation($" -> restarting connection in {interval} seconds ({_retryCount}).");
            _timer.Start();
        }

        private void OnError(object sender, BasicDeliverEventArgs message, bool multiple)
        {
            try
            {
                var channel = (sender as IBasicConsumer)?.Model;

                switch (_queueServiceParams.OnErrorAction)
                {
                    case QueueConfiguration.ErrorAction.DropMessage:
                        {
                            channel.BasicNack(message.DeliveryTag,multiple, false);
                            break;
                        }
                    case QueueConfiguration.ErrorAction.NackOnException:
                        {
                            RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryIntervalInSeconds ?? DefaultRetryInterval));
                            break;
                        }
                    case QueueConfiguration.ErrorAction.RestartConnection:
                    default:
                        {
                            if (channel == null || channel.IsClosed)
                            {
                                RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryIntervalInSeconds ?? DefaultRetryInterval));
                            }
                            else
                            {
                                // let all the other threads catch up so nack occurs once.
                                Thread.Sleep((_queueServiceParams.RetryIntervalInSeconds ?? DefaultRetryInterval) * 1000);

                                channel.BasicNack(message.DeliveryTag, multiple, true);
                            }
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured while trying to handle another error, restarting connection in {_queueServiceParams.RetryIntervalInSeconds ?? DefaultRetryInterval}");
                RestartIn(TimeSpan.FromSeconds(_queueServiceParams.RetryIntervalInSeconds ?? DefaultRetryInterval));
            }
        }



        public void Stop()
        {
            Close();
        }

        private void ReceiveEvent(object sender, BasicDeliverEventArgs args)
        {
            LastWatchDogTicks = DateTime.UtcNow.Ticks;
            var channel = (sender as EventingBasicConsumer)?.Model;
            if (channel == null) throw new ArgumentNullException(nameof(sender), "Model null in received consumer event.");
            
            var message = new BasicMessage(args, channel, _queueServiceParams.QueueName,
                    () => OnError(sender, args, false));

            try
            {
                if (_handler.Process(message))
                {
                    channel.BasicAck(args.DeliveryTag, false);
                }
                // Reset the counter on first successful process
                _retryCount = 0;
            }
            catch (Exception ex)
            {
                // error processing message
                _logger.LogError(ex, $"{ex.Message} -> {args.DeliveryTag}: {args.BasicProperties.MessageId}");
                message.RegisterError.Invoke();
            }
        }

        protected override void OnWatchdogExecution()
        {
            if (LastWatchDogTicks >= DateTime.UtcNow.AddSeconds(-300).Ticks)
            {
                return;
            }

            LastWatchDogTicks = DateTime.UtcNow.AddMinutes(5).Ticks;
            RestartIn(new TimeSpan(0, 0, 10));
        }
    }
}
