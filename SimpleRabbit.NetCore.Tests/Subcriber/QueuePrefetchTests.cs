using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SimpleRabbit.NetCore.Tests.implementations;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Tests
{
    /// <summary>
    /// Testing prefetch of a basic service.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class QueuePrefetchTests : IntegrationFixture
    {
        private uint processCount;
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
            // don't pick up the messages, so no more events come after the prefetch limit is reached
            return false;
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
        }

        [TestCase(1u, 0)]
        [TestCase(1u, 1)]
        [TestCase(1u, 2)]
        [TestCase(3u, 1)]
        [TestCase(3u, 3)]
        [TestCase(3u, 5)]
        [TestCase(5u, 2)]
        [TestCase(5u, 4)]
        [TestCase(5u, 5)]
        [TestCase(5u, 10)]
        public async Task ProcessAllPrefetch(uint prefetch, int numberofMessages)
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = (ushort)prefetch,
                OnErrorAction = QueueConfiguration.ErrorAction.RestartConnection,
                ConsumerTag = "consumer",
                RetryIntervalInSeconds = 10
            };
            var queue = new QueueService(subscriberlogger.Object, ExposedRabbitService.validConfig, queueConfig, handler);

            for (int _ = 0; _ < numberofMessages; _++)
            {
                basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes("message"));
            }
            queue.Start();

            await Task.Delay(RoundTripWaitTime);// let queue pick up messages
            var finalMessageCount = basicService.ExposedChannel.MessageCount(QueueName);
            queue.Dispose();


            if (numberofMessages > prefetch)
            {
                finalMessageCount.Should().Be((uint)numberofMessages - prefetch, "because there are {0} messages with {1} prefetch", numberofMessages, prefetch);
                processCount.Should().Be(prefetch, "because the prefetch is {0}", prefetch);
            }
            // if prefetch is greater than message number, then the final count should be 0.
            else
            {
                finalMessageCount.Should().Be(0, "because the are messages in queue exceeds prefetch");
                processCount.Should().Be((uint)numberofMessages, "because the prefetch exceeds number of messages");
            }

        }

        [TestCase(1u, 0)]
        [TestCase(1u, 1)]
        [TestCase(1u, 2)]
        [TestCase(3u, 1)]
        [TestCase(3u, 3)]
        [TestCase(3u, 5)]
        [TestCase(5u, 2)]
        [TestCase(5u, 4)]
        [TestCase(5u, 5)]
        [TestCase(5u, 6)]
        public async Task DripFeedMessages(uint prefetch, int numberofMessages)
        {
            var queueConfig = new QueueConfiguration
            {
                QueueName = QueueName,
                PrefetchCount = (ushort)prefetch,
                OnErrorAction = QueueConfiguration.ErrorAction.RestartConnection,
                ConsumerTag = "consumer",
                RetryIntervalInSeconds = 10
            };
            var queue = new QueueService(subscriberlogger.Object, ExposedRabbitService.validConfig, queueConfig, handler);
            queue.Start();
            
            for (uint messagecount = 1; messagecount < numberofMessages+1; messagecount++)
            {
                basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, Encoding.UTF8.GetBytes("message"));
                await Task.Delay(RoundTripWaitTime);
                var queueMessageCount = basicService.ExposedChannel.MessageCount(QueueName);

                if (prefetch > messagecount)
                {
                    queueMessageCount.Should().Be(0, "because there are {0} messages with {1} prefetch", messagecount, prefetch);
                    processCount.Should().Be(messagecount, "because the prefetch is {0}", prefetch);
                }
                // if prefetch is greater than message number, then the final count should be 0.
                else
                {
                    queueMessageCount.Should().Be(messagecount - prefetch, "because there are messages in queue exceeds prefetch");
                    processCount.Should().Be(prefetch, "because the prefetch exceeds number of messages");
                }

            }
            queue.Dispose();

        }
    }
}
