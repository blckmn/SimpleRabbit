using NUnit.Framework;
using SimpleRabbit.NetCore.Tests.implementations;
using Subscriber.Service.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Tests
{
    /// <summary>
    /// Provide a basic fixture for testing with a local rabbit mq server.
    /// </summary>
    [Category("Require RabbitMQ")]
    public class IntegrationFixture
    {
        protected const int TimeoutPeriod = 4;
        protected const int RoundTripWaitTime = 500;
        protected static readonly string QueueName = "example";
        protected static readonly string ExchangeName = "MyExchange";
        
        protected MessageProcessor handler;

        /// <summary>
        /// A service linked up to the queue so that it can be managed before each test execution.
        /// </summary>
        protected ExposedRabbitService basicService;

        [OneTimeSetUp]
        public void CreateBasicService()
        {

            handler = new MessageProcessor();
            // Get the channel so it can be purged before each test
            basicService = new ExposedRabbitService(ExposedRabbitService.validConfig);
        }

        [OneTimeTearDown]
        public void DisposeBasicServices()
        {
            basicService.Dispose();
        }

        [SetUp]
        public void EmptyQueue()
        {
            basicService.ExposedChannel.QueuePurge(QueueName);
        }

        protected Task WaitorEndEarly(CancellationToken token)
        {
            return WaitorEndEarly(TimeSpan.FromSeconds(TimeoutPeriod), token);

        }

        protected async Task WaitorEndEarly(TimeSpan timeout, CancellationToken token)
        {
            try
            {
                await Task.Delay(timeout, token);
            }
            catch (TaskCanceledException) { }

        }
    }
}