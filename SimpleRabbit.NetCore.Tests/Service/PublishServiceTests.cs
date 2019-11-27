using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SimpleRabbit.NetCore;
using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using SimpleRabbit.NetCore.Tests.implementations;
using Subscriber.Service.Service;
using System.Threading;
using FluentAssertions;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore.Tests
{
    [TestFixture]
    public class PublishServiceTests
    {
        private const int TimeoutPeriod = 2;
        private static readonly string QueueName = "example";
        private static readonly string ExchangeName = "MyExchange";
        protected CancellationTokenSource tokenSource;
        protected MessageProcessor handler;
        protected PublishService publisher;
        protected QueueService queue;

        private IModel channel;
        [OneTimeSetUp]
        public void Setup()
        {
            var logger = new Mock<ILogger<PublishService>>();
            var loggersub = new Mock<ILogger<QueueService>>();
            var queueConfiguration = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = 1,
                OnErrorAction = QueueConfiguration.ErrorAction.RestartConnection,
                ConsumerTag = "consumer"
            };

            handler = new MessageProcessor();
            publisher = new PublishService(logger.Object,DefaultRabbitService.validConfig);
            queue = new QueueService(loggersub.Object, DefaultRabbitService.validConfig, queueConfiguration, handler);

            // Get the channel so it can be purged before each test
            var basic = new DefaultRabbitService(DefaultRabbitService.validConfig);
            channel = basic.ExposedChannel;
        }

        [SetUp]
        public void EmptyQueue()
        {
            channel.QueuePurge(QueueName);
            tokenSource = new CancellationTokenSource();
            // empty queue
            queue.Start();
        }

        [TearDown]
        public void StopQueue()
        {
            tokenSource.Dispose();
            queue.Stop();
        }

        private async Task WaitorEndEarly(CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(TimeoutPeriod), token);
            }
            catch (Exception) { }

        }

        [Test]
        public async Task SingleMessage()
        {
            var recieved = string.Empty;
            var token = tokenSource.Token;
            var message = "hey";

            void Process(object sender, BasicMessage args)
            {
                recieved = args.Body;
                tokenSource.Cancel();
            };
            handler.Handler += Process;

            publisher.Publish("", QueueName, body: message);

            await WaitorEndEarly(token);

            recieved.Should().NotBeEmpty().And.Be(message);

            //Remove ready for next test
            handler.Handler -= Process;
        }

        [Test]
        public async Task SingleMessageToExchange()
        {
            var recieved = string.Empty;
            var token = tokenSource.Token;
            var message = "hey";

            void Process(object sender, BasicMessage args)
            {
                recieved = args.Body;
                tokenSource.Cancel();
            };
            handler.Handler += Process;

            publisher.ToExchange(ExchangeName, body: message);

            await WaitorEndEarly(token);

            recieved.Should().NotBeEmpty().And.Be(message);

            //Remove ready for next test
            handler.Handler -= Process;
        }

        //[Test]


    }
}