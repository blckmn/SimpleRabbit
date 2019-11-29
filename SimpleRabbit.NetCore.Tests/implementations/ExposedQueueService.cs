using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore.Tests.implementations
{

    public class ExposedQueueService : BaseQueueService
    {

        public ExposedQueueService(ILogger<BaseQueueService> logger, RabbitConfiguration rabbitConfig, QueueConfiguration queueConfig) : base(logger, rabbitConfig, queueConfig)
        {
            Consumer = new ExposedConsumer { Model = ExposedChannel };
        }

        public ConnectionFactory ExposedFactory { get => Factory; }
        public IConnection ExposedConnection { get => Connection; }
        public IModel ExposedChannel { get => Channel; }
        public ExposedConsumer Consumer { get; }

        public new BasicMessage PrepareMessage(object sender, BasicDeliverEventArgs args)
        {
            return base.PrepareMessage(sender, args);
        }


        protected override IBasicConsumer SetUpConsumer()
        {
            return Consumer;
        }
    }
}
