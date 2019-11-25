using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;
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
                Enabled = false,
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

        private Task RestartIn(TimeSpan waitInterval)
        {
            if (_timer.Enabled)
            {
                // another message/thread/task has set the restart.
                return Task.CompletedTask;
            }

            // This needs to be fire and forgotten, as this Task needs to be complete first before it can close the channel
            // All locks need to be released beforehand.
            //https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/341
            Task.Run(() => Close());
            _retryCount++;
            var interval = waitInterval.TotalSeconds * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MAX_RETRY_INTERVAL;
            _timer.Interval = interval * 1000; // seconds
            _logger.LogInformation($" -> restarting queue {_queueServiceParams.QueueName} connection in {interval} seconds ({_retryCount}).");
            _timer.Start();

            return Task.CompletedTask;

        }

        protected Task OnError(object sender, BasicDeliverEventArgs message, bool multiple)
        {
            try
            {
                var channel = (sender as IBasicConsumer)?.Model;

                switch (_queueServiceParams.OnErrorAction)
                {
                    case QueueConfiguration.ErrorAction.DropMessage:
                    {
                        channel.BasicNack(message.DeliveryTag, multiple, false);
                        _logger.LogInformation($" -> dropped message on queue {_queueServiceParams.QueueName}");
                        break;

                    }
                    case QueueConfiguration.ErrorAction.NackOnException:
                    {
                        if (channel == null || channel.IsClosed)
                        {
                            return RestartIn(TimeSpan.FromSeconds(_retryInterval));
                        }
                        else
                        {
                            // let all the other threads catch up so nack occurs once.
                            channel.BasicNack(message.DeliveryTag, multiple, true);
                            var interval = _retryInterval * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MAX_RETRY_INTERVAL;
                            _logger.LogInformation($" -> restarting queue {_queueServiceParams.QueueName} processing in {interval} seconds ({_retryCount}).");
                            return Task.Delay(TimeSpan.FromSeconds(interval));

                        }

                    }
                    case QueueConfiguration.ErrorAction.RestartConnection:
                    default:
                    {
                        return RestartIn(TimeSpan.FromSeconds(_retryInterval));

                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured while trying to handle another error, restarting connection");
                return RestartIn(TimeSpan.FromSeconds(_retryInterval));
            }
            return Task.CompletedTask;
        }

        public void Stop()
        {
            Close();
        }

        /// <summary>
        /// Restart an idle connection every 5 minutes
        /// </summary>
        protected override void OnWatchdogExecution()
        {
            if (LastWatchDogTicks >= DateTime.UtcNow.AddSeconds(-300).Ticks)
            {
                return;
            }


            LastWatchDogTicks = DateTime.UtcNow.AddSeconds(300).Ticks;
            RestartIn(new TimeSpan(0, 0, 10));
        }
    }
}
