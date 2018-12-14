using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleRabbit.NetCore.Dispatcher
{
    public abstract class MessageDispatcher
    {
        private readonly ILogger<MessageDispatcher> _logger;
        private readonly Dictionary<string, List<BasicMessage>> _queues = new Dictionary<string, List<BasicMessage>>();

        protected MessageDispatcher(ILogger<MessageDispatcher> logger)
        {
            _logger = logger;
        }

        public void Enqueue(string key, BasicMessage message, Func<BasicMessage, bool> onProcess, Action<BasicMessage> onError = null)
        {
            lock (_queues)
            {
                if (!_queues.TryGetValue(key, out var queue))
                {
                    queue = new List<BasicMessage>();
                    _queues.Add(key, queue);

                    Task.Run(() => ProcessQueue(key, queue, onProcess, onError));
                }

                queue.Add(message);
            }
        }

        private void ProcessQueue(string key, IList<BasicMessage> queue, Func<BasicMessage, bool> onProcess, Action<BasicMessage> onError = null)
        {
            try
            {
                while (true)
                {
                    BasicMessage message;
                    lock (_queues)
                    {
                        if (queue.Count == 0)
                        {
                            _queues.Remove(key);
                            return;
                        }

                        message = queue[0];
                    }
                    try
                    {
                        if (onProcess.Invoke(message))
                        {
                            message.Channel?.BasicAck(message.DeliveryTag, false);
                        }
                    }
                    catch
                    {
                        onError?.Invoke(message);
                        message?.RegisterError?.Invoke();
                    }

                    lock (_queues)
                    {
                        queue.Remove(message);
                    }
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
