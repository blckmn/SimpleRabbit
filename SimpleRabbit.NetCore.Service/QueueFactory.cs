using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SimpleRabbit.NetCore.Service
{
    public class QueueFactory : IHostedService
    {
        private readonly ILogger<QueueFactory> _logger;
        /// <summary>
        /// A list of the cluster configurations.
        /// This is a hack.
        /// </summary>
        private readonly List<Subscribers> _subscribers;
        private readonly IOptionsMonitor<List<QueueConfiguration>> _queueconfig;
        private readonly IOptionsMonitor<RabbitConfiguration> _rabbitconfig;
        private readonly IServiceProvider _provider;
        private readonly IEnumerable<IMessageHandler> _handlers;
        private readonly Dictionary<string,List<IQueueService>> _queueServices = new Dictionary<string,List<IQueueService>>();

        public QueueFactory(ILogger<QueueFactory> logger, 
            IOptions<List<Subscribers>> subscribers, 
            IOptionsMonitor<List<QueueConfiguration>> queueconfig, 
            IOptionsMonitor<RabbitConfiguration> rabbitconfig,
            IServiceProvider provider, IEnumerable<IMessageHandler> handlers)
        {
            _logger = logger;
            _subscribers = subscribers.Value;
            _queueconfig = queueconfig;
            _rabbitconfig = rabbitconfig;
            _provider = provider;
            _handlers = handlers;

            queueconfig.OnChange((config, name) =>
            {
                //Recreate all queues (this is inefficient, should perform a check on changed configurations).

                // Using names, as it will specify to which cluster.
                KillQueues(name);
                CreateQueues(name);
            });
        }

        /// <summary>
        /// Create the queues for a specific cluster/ <see cref="RabbitConfiguration"/>
        /// </summary>
        /// <param name="name">the name of the cluster as defined in Configuration</param>
        public void CreateQueues(string name)
        {
            var rabbitconfig = _rabbitconfig.Get(name);
            var queues = _queueconfig.Get(name);

            var queueList = new List<IQueueService>();

            foreach (var queue in queues)
            {
                var handler = _handlers.FirstOrDefault(s => s.CanProcess(queue.ConsumerTag));

                if (handler == null)
                {
                    _logger.LogError($"no handler for queue {queue.QueueName}, {queue.ConsumerTag}");
                    continue;
                }

                var queueService = new QueueService(rabbitconfig, _provider.GetService<ILogger<QueueService>>());

                queueService.Start(queue, handler);
                queueList.Add(queueService);
                _logger.LogInformation($"Added subscriber -> Queue:{queue.QueueName}, Tag:{queue.ConsumerTag}");
            }

            _queueServices.Add(name, queueList);
           
        }

        public void KillQueues(string name)
        {
            var queues = _queueServices[name];

            foreach (var queue in queues)
            {
                queue.Stop();
            }

            _queueServices.Remove(name);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hosted subscriber management service starting");
            foreach (var name in _subscribers)
            {
                CreateQueues(name.Name);
                
            }
            _logger.LogInformation("Hosted subscriber management service started");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hosted subscriber management service stopping");
            foreach (var name in _subscribers)
            {
                KillQueues(name.Name);

            }
            _logger.LogInformation("Hosted subscriber management service stopped");

            return Task.CompletedTask;
        }
    }
}
