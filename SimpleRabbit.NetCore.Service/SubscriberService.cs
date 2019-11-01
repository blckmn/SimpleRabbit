using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleRabbit.NetCore.Service
{
    public class SubscriberService : IHostedService
    {
        private readonly ILogger<SubscriberService> _logger;
        private readonly IServiceProvider _provider;
        private readonly IList<SubscriberConfiguration> _subscribers;

        public SubscriberService(ILogger<SubscriberService> logger, List<SubscriberConfiguration> options, IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;
            _subscribers = options;
        }

        private readonly List<IQueueService> _queueServices = new List<IQueueService>();
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hosted subscriber management service starting");

            if (_subscribers == null) return Task.CompletedTask;

            var consumers = _provider.GetServices<IMessageHandler>().ToList();
            foreach (var subscriber in _subscribers)
            {
                var queueService = _provider.GetService<IQueueService>();

                queueService.Start(subscriber, consumers.FirstOrDefault(c => c.CanProcess(subscriber.ConsumerTag)));
                _queueServices.Add(queueService);
                _logger.LogInformation($"Added subscriber -> Queue:{subscriber.QueueName}, Tag:{subscriber.ConsumerTag}");
            }

            _logger.LogInformation("Hosted subscriber management service started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hosted subscriber management service stopping");

            foreach (var queueService in _queueServices)
            {
                queueService.Stop();
            }

            _queueServices.Clear();

            _logger.LogInformation("Hosted subscriber management service stopped");
            return Task.CompletedTask;
        }
    }
}
