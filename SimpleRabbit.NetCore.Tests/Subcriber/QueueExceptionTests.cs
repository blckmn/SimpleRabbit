using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SimpleRabbit.NetCore.Tests.implementations;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Tests
{
    public class TestMessages
    {
        public const string Exception = "Exception";
        public const string Redeliver = "Redeliver";
        public const string Pass = "Pass";
    }
    /// <summary>
    /// Testing <see cref="BaseQueueService"/> error behaviour
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class QueueExceptionTests : IntegrationFixture
    {
        private int successCount;
        private int errorCount;
        private int processCount;
        protected Mock<ILogger<QueueService>> subscriberlogger;
        [OneTimeSetUp]
        public void AddHandlerEvent()
        {
            subscriberlogger = new Mock<ILogger<QueueService>>();
            handler.Handler = Process;
        }

        private bool Process(BasicMessage args)
        {
            processCount++;
            if (args.Body.Equals(TestMessages.Exception) || // always throw exception
                (args.Body.Equals(TestMessages.Redeliver) && !args.DeliveryArgs.Redelivered)) //first time fail, second pass
            {
                errorCount++;
                throw new Exception("error");
            }
            successCount++;
            return true;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            handler.Handler -= Process;
        }

        [SetUp]
        public void ResetFlag()
        {
            processCount = 0;
            errorCount = 0;
            successCount = 0;
        }

        [Test]
        public async Task RestartOnConnection()
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = 1,
                OnErrorAction = QueueConfiguration.ErrorAction.RestartConnection,
                ConsumerTag = "consumer",
                RetryIntervalInSeconds = 2
            };
            var queue = new QueueService(ExposedRabbitService.validConfig, subscriberlogger.Object);
            queue.Start(queueConfig, handler);

            var initialConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);

            basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes(TestMessages.Redeliver));
            await Task.Delay(RoundTripWaitTime);

            var inbetweenConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);

            await Task.Delay(2000 + RoundTripWaitTime); // wait retry interval + RoundTrip

            var finalConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);

            queue.Dispose();

            successCount.Should().Be(1, "because item is not excepted twice");
            processCount.Should().Be(2, "because first time exception, second time succesful process");
            initialConsumerCount.Should().Be(1);
            inbetweenConsumerCount.Should().Be(0, "because the channel restarts");
            errorCount.Should().Be(1, "because error should be thrown first time");
            finalConsumerCount.Should().Be(1);
        }

        [Test]
        public async Task NackOnException()
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = 1,
                OnErrorAction = QueueConfiguration.ErrorAction.NackOnException,
                ConsumerTag = "consumer",
                RetryIntervalInSeconds = 1
            };
            var queue = new QueueService(ExposedRabbitService.validConfig, subscriberlogger.Object);
            queue.Start(queueConfig, handler);

            var initialConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);
            basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes(TestMessages.Redeliver));
            await Task.Delay(RoundTripWaitTime);

            var inbetweenConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);

            await Task.Delay(1000+RoundTripWaitTime);

            var finalConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);

            queue.Dispose();

            successCount.Should().Be(1, "because item is not excepted twice");
            processCount.Should().Be(2, "because first time exception, second time succesful process");
            initialConsumerCount.Should().Be(1);
            inbetweenConsumerCount.Should().Be(1, "because only the message is nacked");
            errorCount.Should().Be(1, "because error should be thrown first time");
            finalConsumerCount.Should().Be(1);
        }

        [Test]
        public async Task DropMessage()
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = 1,
                OnErrorAction = QueueConfiguration.ErrorAction.DropMessage,
                ConsumerTag = "consumer",
                RetryIntervalInSeconds = 1
            };
            var queue = new QueueService(ExposedRabbitService.validConfig, subscriberlogger.Object);
            queue.Start(queueConfig, handler);

            var initialConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);
            basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes(TestMessages.Redeliver));
            await Task.Delay(RoundTripWaitTime);

            var finalConsumerCount = basicService.ExposedChannel.ConsumerCount(QueueName);
            queue.Dispose();

            processCount.Should().Be(1, "because the message is dropped");
            initialConsumerCount.Should().Be(1);
            errorCount.Should().Be(1);
            finalConsumerCount.Should().Be(1, "because consumer shouldn't have stopped");
        }


        [Test]
        public async Task LetInFlightMessagesContinue()
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = 3,
                OnErrorAction = QueueConfiguration.ErrorAction.RestartConnection,
                ConsumerTag = "consumer",
                RetryIntervalInSeconds = 10
            };
            var queue = new QueueService(ExposedRabbitService.validConfig, subscriberlogger.Object);
            queue.Start(queueConfig, handler);
            basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes(TestMessages.Pass));
            basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes(TestMessages.Exception));
            basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes(TestMessages.Pass));
            await Task.Delay(RoundTripWaitTime);

            queue.Dispose();
            var finalMessageCount = basicService.ExposedChannel.MessageCount(QueueName);

            processCount.Should().Be(3, "because 3 messages were sent");
            successCount.Should().Be(2, "because 2 successful messages were sent");
            errorCount.Should().Be(1);
            finalMessageCount.Should().Be(1, "because only one message caused exception");
        }


        [Test]
        public async Task ReleaseHeldMessages()
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = 1,
                OnErrorAction = QueueConfiguration.ErrorAction.RestartConnection,
                ConsumerTag = "consumer",
                RetryIntervalInSeconds = 10
            };
            var queue = new QueueService(ExposedRabbitService.validConfig, subscriberlogger.Object);
            queue.Start(queueConfig, handler);
            basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes(TestMessages.Exception));
            await Task.Delay(RoundTripWaitTime);

            var freeMessages = basicService.ExposedChannel.MessageCount(QueueName);

            queue.Dispose();
            var finalMessageCount = basicService.ExposedChannel.MessageCount(QueueName);

            processCount.Should().Be(1, "because 1 messages were sent");
            successCount.Should().Be(0, "because 0 successful messages were sent");
            errorCount.Should().Be(1);
            freeMessages.Should().Be(1, "because excepted messages should be released");
            finalMessageCount.Should().Be(1, "because only one message caused exception");
        }
    }
}
