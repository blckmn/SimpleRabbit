using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleRabbit.NetCore.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleRabbit.NetCore.Tests.implementations
{
    public class ExposedQueueFactory : QueueFactory
    {
        public Dictionary<string, List<IQueueService>> Queues { get => _queueServices; }
        public ExposedQueueFactory(ILogger<QueueFactory> logger, IOptions<List<Subscribers>> subscribers, IOptionsMonitor<List<QueueConfiguration>> queueconfig, IOptionsMonitor<RabbitConfiguration> rabbitconfig, IServiceProvider provider, IEnumerable<IMessageHandler> handlers) : base(logger, subscribers, queueconfig, rabbitconfig, provider, handlers)
        {
        }

        public override IQueueService CreateQueue(RabbitConfiguration config)
        {
            return new StubQueueService(config);
        }
    }
}
