using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore.Dispatcher
{
    public abstract class AbstractDispatchingMessageHandler<T> : IMessageHandler, IChannelOptions
    {
        private readonly ILogger<AbstractDispatchingMessageHandler<T>> _logger;
        private readonly Dictionary<string, List<ModelDetails<T>>> _queues = new Dictionary<string, List<ModelDetails<T>>>();
        private readonly object _semaphore = new object();

        private IModel _model;
        private Action _onError;

        protected AbstractDispatchingMessageHandler(ILogger<AbstractDispatchingMessageHandler<T>> logger)
        {
            _logger = logger;
        }

        public abstract bool CanProcess(string tag);
        protected abstract void ProcessMessage(T msg);
        public abstract T Get(BasicDeliverEventArgs args);
        public abstract string GetKey(T msg);

        public bool Process(BasicDeliverEventArgs args)
        {
            var msg = Get(args);
            var key = GetKey(msg);
            lock (_semaphore)
            {
                if (!_queues.TryGetValue(key, out var queue))
                {
                    queue = new List<ModelDetails<T>>();
                    _queues.Add(key, queue);

                    Task.Run(() => ProcessQueue(queue, key));
                }
                queue.Add(new ModelDetails<T>
                {
                    Args = args,
                    Message = msg
                });
            }

            return false;
        }

        private void ProcessQueue(List<ModelDetails<T>> queue, string key)
        {
            try
            {
                while (true)
                {
                    ModelDetails<T> details;
                    lock (_semaphore)
                    {
                        if (queue.Count == 0)
                        {
                            _queues.Remove(key);
                            return;
                        }

                        details = queue[0];
                    }

                    ProcessMessage(details.Message);

                    lock (_semaphore)
                    {
                        queue.Remove(details);
                    }

                    _model?.BasicAck(details.Args.DeliveryTag, false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while processing a message queue");
                queue.Clear();
                _queues.Remove(key);
                _onError?.Invoke();
            }
        }

        public void SetChannelOptions(IModel model, Action onError)
        {
            _model = model;
            _onError = onError;
        }
    }
}
