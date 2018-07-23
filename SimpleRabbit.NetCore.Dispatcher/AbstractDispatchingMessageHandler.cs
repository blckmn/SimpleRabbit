using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleRabbit.NetCore.Dispatcher
{
    public abstract class AbstractDispatchingMessageHandler<T> : IMessageHandler
    {
        private readonly ILogger<AbstractDispatchingMessageHandler<T>> _logger;
        private readonly Dictionary<string, List<ModelDetails<T>>> _queues = new Dictionary<string, List<ModelDetails<T>>>();
        private readonly object _semaphore = new object();

        protected AbstractDispatchingMessageHandler(ILogger<AbstractDispatchingMessageHandler<T>> logger)
        {
            _logger = logger;
        }

        public abstract bool CanProcess(string tag);
        protected abstract void ProcessMessage(T message);
        public abstract T GetItem(BasicMessage message);
        public abstract string GetKey(T message);

        public bool Process(BasicMessage message)
        {
            var item = GetItem(message);
            var key = GetKey(item);
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
                    Message = message,
                    Item = item
                });
            }

            return false;
        }

        private void ProcessQueue(IList<ModelDetails<T>> queue, string key)
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
                    try
                    {
                        ProcessMessage(details.Item);
                    }
                    catch
                    {
                        details?.Message?.RegisterError?.Invoke();
                        throw;
                    }

                    lock (_semaphore)
                    {
                        queue.Remove(details);
                    }

                    details.Message.Channel?.BasicAck(details.Message.DeliveryTag, false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while processing a message queue");
                queue.Clear();
                _queues.Remove(key);
            }
        }
    }
}
