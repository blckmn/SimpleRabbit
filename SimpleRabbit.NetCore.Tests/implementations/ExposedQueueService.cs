using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore.Tests.implementations
{

    public class ExposedQueueService : QueueService
    {

        public ExposedQueueService(ILogger<QueueService> logger, RabbitConfiguration rabbitConfig) : base(rabbitConfig, logger)
        {
            Consumer = new ExposedConsumer { Model = ExposedChannel };
        }

        public ConnectionFactory ExposedFactory { get => Factory; }
        public IConnection ExposedConnection { get => Connection; }
        public IModel ExposedChannel { get => Channel; }
        public ExposedConsumer Consumer { get; }
    }
}
