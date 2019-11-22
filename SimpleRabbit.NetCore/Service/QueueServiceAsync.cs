using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore
{

    public class QueueServiceAsync : BaseQueueService, IQueueService
    {
        private readonly IMessageHandlerAsync _handler;
        public QueueServiceAsync(ILogger<QueueServiceAsync> logger, RabbitConfiguration rabbitConfig, QueueConfiguration queueConfig, IMessageHandlerAsync handler)
           : base(logger, rabbitConfig, queueConfig)
        {
            _handler = handler;
        }

        protected override IBasicConsumer SetUpConsumer()
        {
            Factory.DispatchConsumersAsync = true;
            var consumer = new AsyncEventingBasicConsumer(Channel);
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


        private async Task ReceiveEvent(object sender, BasicDeliverEventArgs args)
        {
            BasicReceiveEvent(sender, args);

            var message = PrepareMessage(sender, args);

            try
            {
                if (await _handler.Process(message))
                {
                    message.Ack();
                }
                ResetRetryCounter();
            }
            catch (Exception ex)
            {
                // error processing message
                _logger.LogError(ex, $"{ex.Message} -> {args.DeliveryTag}: {args.BasicProperties.MessageId}");
                message?.ErrorAction?.Invoke();
            }
        }

    }
}
