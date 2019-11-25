using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace SimpleRabbit.NetCore
{

    public class QueueService : BaseQueueService, IQueueService
    {
        private readonly IMessageHandler _handler;
        public QueueService(ILogger<QueueService> logger, RabbitConfiguration rabbitConfig, QueueConfiguration queueConfig, IMessageHandler handler)
            : base(logger, rabbitConfig, queueConfig)
        {
            _handler = handler;
        }

        protected override IBasicConsumer SetUpConsumer()
        {
            Factory.DispatchConsumersAsync = false;

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += ReceiveEvent;
            return consumer;

        }

        public override void Start()
        {
            if (_handler == null)
            {
                throw new ArgumentNullException(nameof(_handler), $"No handler provided for {_queueServiceParams.ConsumerTag} => {_queueServiceParams.QueueName}");
            }
            base.Start();
        }

        private void ReceiveEvent(object sender, BasicDeliverEventArgs args)
        {

            var message = PrepareMessage(sender, args);

            try
            {
                if (_handler.Process(message))
                {
                    message.Ack();
                }
                ResetRetryCounter();
            }
            catch (Exception ex)
            {
                // error processing message
                _logger.LogError(ex, $"{ex.Message} -> {args.DeliveryTag}: {args.BasicProperties.MessageId}");


                //TODO Investigate semantic difference between the two method calls
                // https://stackoverflow.com/questions/41140107/task-run-vs-invoke-difference

                //if (message?.ErrorAction != null) Task.Run(message?.ErrorAction).GetAwaiter().GetResult();
                message?.ErrorAction?.Invoke().GetAwaiter().GetResult();
            }
        }

    }
}
