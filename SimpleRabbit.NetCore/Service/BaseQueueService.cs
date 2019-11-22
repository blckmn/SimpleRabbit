using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using Timer = System.Timers.Timer;

namespace SimpleRabbit.NetCore
{
    /// <summary>
    /// Base class for queues, Keeping a queue alive and providing control logic behaviour.
    /// </summary>
    /// <remarks>
    /// Refer to implementations for consumption logic.
    /// <seealso cref="QueueService"/>
    /// <seealso cref="QueueServiceAsync"/>
    /// 
    /// 
    /// </remarks>
    /// Due to EventHandlers only being present in <see cref="EventingBasicConsumer"/> and <see cref="AsyncEventingBasicConsumer"/>
    /// This class does not add to the event handler list, requiring base class implementations to call the following where appropriate
    /// <see cref="BasicReceiveEvent"/>
    /// <see cref="ResetRetryCounter"/>
    /// 
    public abstract class BaseQueueService : BasicRabbitService
    {
        private const int DEFAULT_RETRY_INTERVAL = 15;
        private const int MAX_RETRY_INTERVAL = 120;
        private readonly Timer _timer;

        protected readonly ILogger<BaseQueueService> _logger;
        /// <summary>
        /// parameters of the queue creation.
        /// </summary>
        protected readonly QueueConfiguration _queueServiceParams;

        private readonly int _retryInterval;

        private int _retryCount;

        public BaseQueueService(ILogger<BaseQueueService> logger, RabbitConfiguration rabbitConfig, QueueConfiguration queueConfig) : base(rabbitConfig)
        {
            _logger = logger;
            _queueServiceParams = queueConfig;

            _retryInterval = _queueServiceParams.RetryIntervalInSeconds ?? DEFAULT_RETRY_INTERVAL;


            _timer = new Timer
            {
                AutoReset = false,
            };

            _timer.Elapsed += (sender, args) =>
            {
                _timer.Stop();
                StartQueue();
            };
        }

        public virtual void Start()
        {
            StartQueue();
        }

        private void StartQueue()
        {
            Stop();
            try
            {
                var consumer = SetUpConsumer();
                Channel.BasicQos(0, _queueServiceParams.PrefetchCount ?? 1, false);
                Channel.BasicConsume(_queueServiceParams.QueueName, false, _queueServiceParams.ConsumerName ?? _queueServiceParams.ConsumerTag, consumer);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{_queueServiceParams.QueueName} -> {e.Message}");
                RestartIn(TimeSpan.FromSeconds(_retryInterval));
            }

        }

        protected abstract IBasicConsumer SetUpConsumer();


        /// <summary>
        /// Create a <see cref="BasicMessage"/> with Ack and Nack abilities.
        /// </summary>
        /// <param name="sender"> The consumer where the message came from</param>
        /// <param name="args"> the message payload that arrived</param>
        /// <returns></returns>
        protected BasicMessage PrepareMessage(object sender, BasicDeliverEventArgs args)
        {
            var channel = (sender as IBasicConsumer)?.Model;
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(sender), "Model null in received consumer event.");
            }

            var message = new BasicMessage(args, channel, _queueServiceParams.QueueName,
                    () => OnError(sender, args, false));

            return message;
        }


        protected void BasicReceiveEvent(object sender, BasicDeliverEventArgs args)
        {
            LastWatchDogTicks = DateTime.UtcNow.Ticks;
        }

        protected void ResetRetryCounter()
        {
            _retryCount = 0;
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
            var interval = waitInterval.TotalSeconds * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MAX_RETRY_INTERVAL;

            _timer.Interval = interval * 1000; // seconds
            _logger.LogInformation($" -> restarting connection in {interval} seconds ({_retryCount}).");
            _timer.Start();
        }

        protected void OnError(object sender, BasicDeliverEventArgs message, bool multiple)
        {
            try
            {
                var channel = (sender as IBasicConsumer)?.Model;

                switch (_queueServiceParams.OnErrorAction)
                {
                    case QueueConfiguration.ErrorAction.DropMessage:
                    {
                        channel.BasicNack(message.DeliveryTag, multiple, false);
                        break;
                    }
                    case QueueConfiguration.ErrorAction.NackOnException:
                    {
                        RestartIn(TimeSpan.FromSeconds(_retryInterval));
                        break;
                    }
                    case QueueConfiguration.ErrorAction.RestartConnection:
                    default:
                    {
                        if (channel == null || channel.IsClosed)
                        {
                            RestartIn(TimeSpan.FromSeconds(_retryInterval));
                        }
                        else
                        {
                            // let all the other threads catch up so nack occurs once.
                            Thread.Sleep((_queueServiceParams.RetryIntervalInSeconds ?? DEFAULT_RETRY_INTERVAL) * 1000);

                            channel.BasicNack(message.DeliveryTag, multiple, true);
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured while trying to handle another error, restarting connection");
                RestartIn(TimeSpan.FromSeconds(_retryInterval));
            }
        }

        public void Stop()
        {
            Close();
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
