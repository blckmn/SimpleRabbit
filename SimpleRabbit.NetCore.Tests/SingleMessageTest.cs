using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SimpleRabbit.NetCore.Tests.implementations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Tests
{
    /// <summary>
    /// Full integration (publsiher + subscriber) with rabbitMQ server.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class SingleMessageTest : IntegrationFixture
    {
        protected PublishService publisher;
        protected QueueService queue;
        private BasicMessage recieved;
        protected CancellationTokenSource tokenSource;
        protected Mock<ILogger<PublishService>> publisherlogger;
        protected Mock<ILogger<QueueService>> subscriberlogger;
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            publisherlogger = new Mock<ILogger<PublishService>>();
            subscriberlogger = new Mock<ILogger<QueueService>>();
            publisher = new PublishService(publisherlogger.Object, ExposedRabbitService.validConfig);
           
            queue = new QueueService(ExposedRabbitService.validConfig, subscriberlogger.Object);
            handler.Handler = Process;
        }

        private Acknowledgement Process(BasicMessage args)
        {
            recieved = args;
            tokenSource.Cancel();
            return Acknowledgement.Ack;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            publisher.Dispose();
            queue.Dispose();
            handler.Handler = null;
        }

        [SetUp]
        public void StartQueue()
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = 1,
                OnErrorAction = QueueConfiguration.ErrorAction.RestartConnection,
                ConsumerTag = "consumer"
            };
            tokenSource = new CancellationTokenSource();
            queue.Start(queueConfig, handler);

        }

        [TearDown]
        public void StopQueue()
        {
            
            queue.Stop();
            tokenSource.Dispose();
        }


        [Test]
        public async Task PublishEmptyMessage()
        {

            publisher.Publish(ExchangeName, null);

            await WaitorEndEarly(tokenSource.Token);

            recieved.Body.Should().BeEmpty();
        }

        [Test]
        public async Task PublishDirect()
        {
            var message = "hey";

            publisher.Publish("", QueueName, body: message);

            await WaitorEndEarly(tokenSource.Token);

            recieved.Body.Should().Be(message);
        }

        [Test]
        public async Task PublishWithExchange()
        {
            var message = "hey";

            publisher.Publish(ExchangeName, null, body: message);

            await WaitorEndEarly(tokenSource.Token);

            recieved.Body.Should().Be(message);
        }

        [Test]
        public async Task ToExchange()
        {
            var message = "hey";

            publisher.ToExchange(ExchangeName, body: message);

            await WaitorEndEarly(tokenSource.Token);

            recieved.Body.Should().Be(message);
        }

        [Test]
        public async Task PublishWithProperties()
        {
            var message = "hey";
            var list = new List<string> { "bob", "alice", "charles" };
            var properties = publisher.GetBasicProperties();
            properties.AppId = "App";
            properties.Headers = new Dictionary<string, object>
            {
                { "hi", list }
            };


            publisher.Publish(ExchangeName, null, properties, message);

            await WaitorEndEarly(tokenSource.Token);

            recieved.Body.Should().Be(message);
            recieved.Properties.AppId.Should().Be(properties.AppId);
            recieved.Headers.Should().ContainKey("hi")
                .WhoseValue.Should().BeEquivalentTo(list.Select(s => Encoding.UTF8.GetBytes(s)));
        }


        [Test]
        public async Task PublishWithException()
        {
            var message = "exception";
            publisher.Publish(ExchangeName, null, body: message);

            await WaitorEndEarly(tokenSource.Token);

            recieved.Body.Should().Be(message);
        }


    }
}