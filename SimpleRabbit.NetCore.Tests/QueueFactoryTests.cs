using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SimpleRabbit.NetCore.Service;
using SimpleRabbit.NetCore.Tests.implementations;
using Subscriber.Service.Service;
using System.Collections.Generic;
using System.Linq;

namespace SimpleRabbit.NetCore.Tests
{
    [TestFixture]
    public class QueueFactoryTests
    {
        [Test]

        public void TransientHandler()
        {
            var mockLog = new Mock<ILogger<QueueFactory>>();
            var subConfig = new Mock<IOptions<List<Subscribers>>>();
            subConfig
                .Setup(x => x.Value)
                .Returns(
                    new List<Subscribers>() { 
                        new Subscribers { Name = "first"},
                        new Subscribers{Name = "second"} 
                    }
                );
            var queueConfig = new Mock<IOptionsMonitor<List<QueueConfiguration>>>();
            queueConfig
                .Setup(x=>x.Get(It.IsAny<string>()))
                .Returns(new List<QueueConfiguration>(){
                    new QueueConfiguration
                    {
                        QueueName = "first",
                    },
                    new QueueConfiguration
                    {
                        QueueName = "second",
                    }
                }
                );

            var rabbitConfig = new Mock<IOptionsMonitor<RabbitConfiguration>>();

            rabbitConfig.Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new RabbitConfiguration { Name = "config" });

            var provider = new ServiceCollection()
               .AddTransientMessageHandler<MessageProcessor>()
               .BuildServiceProvider();

            var handlers = provider.GetServices<IMessageHandler>();

            var service = new ExposedQueueFactory(mockLog.Object,subConfig.Object,queueConfig.Object,rabbitConfig.Object,provider,handlers);

            service.StartQueues("default");

            var queues = service.Queues;

            var outputHandlers = queues.Values.SelectMany(x => x).Select(x=>(StubQueueService)x).ToList();

            outputHandlers.Count().Should().Be(2, "because only 2 queues are registered");

            var firstHandler = outputHandlers[0].Handler;
            var secondHandler = outputHandlers[1].Handler;

            firstHandler.Should().NotBeNull();
            secondHandler.Should().NotBeNull();
            firstHandler.Should().NotBeSameAs(secondHandler);
        }

        [Test]
        public void SingletonHandler()
        {
            var mockLog = new Mock<ILogger<QueueFactory>>();
            var subConfig = new Mock<IOptions<List<Subscribers>>>();
            subConfig
                .Setup(x => x.Value)
                .Returns(
                    new List<Subscribers>() {
                        new Subscribers { Name = "first"},
                        new Subscribers{Name = "second"}
                    }
                );
            var queueConfig = new Mock<IOptionsMonitor<List<QueueConfiguration>>>();
            queueConfig
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new List<QueueConfiguration>(){
                    new QueueConfiguration
                    {
                        QueueName = "first",
                    },
                    new QueueConfiguration
                    {
                        QueueName = "second",
                    }
                }
                );

            var rabbitConfig = new Mock<IOptionsMonitor<RabbitConfiguration>>();

            rabbitConfig.Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new RabbitConfiguration { Name = "config" });

            var provider = new ServiceCollection()
               .AddSingletonMessageHandler<MessageProcessor>()
               .BuildServiceProvider();

            var handlers = provider.GetServices<IMessageHandler>();

            var service = new ExposedQueueFactory(mockLog.Object, subConfig.Object, queueConfig.Object, rabbitConfig.Object, provider, handlers);

            service.StartQueues("default");

            var queues = service.Queues;

            var outputHandlers = queues.Values.SelectMany(x => x).Select(x => (StubQueueService)x).ToList();

            outputHandlers.Count().Should().Be(2, "because only 2 queues are registered");

            var firstHandler = outputHandlers[0].Handler;
            var secondHandler = outputHandlers[1].Handler;

            firstHandler.Should().NotBeNull();
            secondHandler.Should().NotBeNull();
            firstHandler.Should().BeSameAs(secondHandler);
        }
    }






}
