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
            if (_queueServiceParams.ConsumerTag == null)
            {
                throw new ArgumentNullException(nameof(_queueServiceParams.ConsumerTag), "consumer tag cannot be null");
            }
            StartQueue();
        }

        private void StartQueue()
        {
            Stop();
            try
            {
                var consumer = SetUpConsumer();
                Channel.BasicQos(0, _queueServiceParams.PrefetchCount ?? 1, false);
                Channel.BasicConsume(_queueServiceParams.QueueName, false, _queueServiceParams.ConsumerTag, consumer);
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

        protected void ResetRetryCounter()
        {
            _retryCount = 0;
        }

        private void RestartIn(TimeSpan waitInterval)
        {
            if (_timer.Enabled)
            {
                // another message/thread/task has set the restart.
                return;
            }

            //take note of blocking if clearing connection here
            //https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/341

            //Only cancel subscription, In flight messages are still processed
            Channel.BasicCancel(_queueServiceParams.ConsumerTag);
            _retryCount++;
            var interval = waitInterval.TotalSeconds * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MAX_RETRY_INTERVAL;
            _timer.Interval = interval * 1000; // seconds
            _logger.LogInformation($" -> restarting queue {_queueServiceParams.QueueName} connection in {interval} seconds ({_retryCount}).");
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
                        // maybe have it check whether a message is redelivered first, then drop, i.e it needs to fail a number of times.
                        channel.BasicNack(message.DeliveryTag, multiple, false);
                        _logger.LogInformation($" -> dropped message on queue {_queueServiceParams.QueueName}");
                        break;

                    }
                    case QueueConfiguration.ErrorAction.NackOnException:
                    {
                        if (channel == null || channel.IsClosed)
                        {
                            RestartIn(TimeSpan.FromSeconds(_retryInterval));
                        }
                        else
                        {
                            // the particular thread is blocked.
                            // let all the other threads catch up so nack occurs once.
                            channel.BasicNack(message.DeliveryTag, multiple, true);
                            var interval = _retryInterval * (_queueServiceParams.AutoBackOff ? _retryCount : 1) % MAX_RETRY_INTERVAL;
                            _logger.LogInformation($" -> restarting queue {_queueServiceParams.QueueName} processing in {interval} seconds ({_retryCount}).");
                            Thread.Sleep(TimeSpan.FromSeconds(interval));
                        }
                        return;

                    }
                    case QueueConfiguration.ErrorAction.RestartConnection:
                    default:
                    {
                        RestartIn(TimeSpan.FromSeconds(_retryInterval));
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured while trying to handle another error, restarting connection");
                RestartIn(TimeSpan.FromSeconds(_retryInterval));
                return;
            }
        }

        public void Stop()
        {
            Close();
        }
    }
}
