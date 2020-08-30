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
        protected readonly ILogger<QueueFactory> _logger;
        /// <summary>
        /// A list of the cluster configurations.
        /// This is a hack.
        /// </summary>
        protected readonly List<Subscribers> _subscribers;
        protected readonly IOptionsMonitor<List<QueueConfiguration>> _queueconfig;
        protected readonly IOptionsMonitor<RabbitConfiguration> _rabbitconfig;
        protected readonly IServiceProvider _provider;
        protected readonly IEnumerable<IMessageHandler> _handlers;
        protected readonly Dictionary<string,List<IQueueService>> _queueServices = new Dictionary<string,List<IQueueService>>();

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
                StopQueues(name);
                StartQueues(name);
            });
        }

        /// <summary>
        /// Create the queues for a specific cluster/ <see cref="RabbitConfiguration"/>
        /// </summary>
        /// <param name="name">the name of the cluster as defined in Configuration</param>
        public void StartQueues(string name)
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
                var queueService = CreateQueue(rabbitconfig);

                var newHandler = GetHandlerInstance(handler);

                queueService.Start(queue,newHandler);
                queueList.Add(queueService);
                _logger.LogInformation($"Added subscriber -> Queue:{queue.QueueName}, Tag:{queue.ConsumerTag}");
            }

            _queueServices.Add(name, queueList);
           
        }

        private IMessageHandler GetHandlerInstance(IMessageHandler handler)
        {
            var newHandler = _provider.GetService(handler.GetType());
            if( newHandler == null)
            {
                // no transient registration of self, assume singleton.
                return handler;
            }
            return newHandler as IMessageHandler;
        }

        //default behaviour of queue service.
        public virtual IQueueService CreateQueue(RabbitConfiguration config)
        {
            return new QueueService(config, _provider.GetService<ILogger<QueueService>>());
        }

        public void StopQueues(string name)
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
                StartQueues(name.Name);
                
            }
            _logger.LogInformation("Hosted subscriber management service started");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hosted subscriber management service stopping");
            foreach (var name in _subscribers)
            {
                StopQueues(name.Name);

            }
            _logger.LogInformation("Hosted subscriber management service stopped");

            return Task.CompletedTask;
        }
    }
}
