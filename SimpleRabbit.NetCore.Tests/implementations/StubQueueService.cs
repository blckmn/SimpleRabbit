using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore.Tests.implementations
{

    public class StubQueueService : IQueueService
    {

        public IMessageHandler Handler { get; set; }

        public QueueConfiguration SubscriberConfig { get; set; }
        public RabbitConfiguration RabbitConfig { get; set; }

        public StubQueueService(RabbitConfiguration config)
        {
            RabbitConfig = config;
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public IConnection GetConnection()
        {
            return null;
        }

        public IBasicProperties GetBasicProperties()
        {
            throw new System.NotImplementedException();
        }

        public void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1)
        {
            SubscriberConfig = new QueueConfiguration
            {
                PrefetchCount = prefetch,
                QueueName = queue,
                ConsumerTag = tag,
            };
            Handler = handler;
        }

        public void Start(QueueConfiguration subscriberConfiguration, IMessageHandler handler)
        {
            SubscriberConfig = subscriberConfiguration;
            Handler = handler;
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }



        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
