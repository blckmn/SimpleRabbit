using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleRabbit.NetCore.Dispatcher
{
    public class MessageDispatcher
    {
        private readonly ILogger<MessageDispatcher> _logger;
        private readonly Dictionary<string, List<BasicMessage>> _queues = new Dictionary<string, List<BasicMessage>>();

        public MessageDispatcher(ILogger<MessageDispatcher> logger)
        {
            _logger = logger;
        }

        public void Init(Func<BasicMessage, bool> onProcess, Action<BasicMessage> onError = null)
        {
            OnProcess = onProcess;
            OnError = onError;
        }

        public Action<BasicMessage> OnError { get; set; }
        public Func<BasicMessage, bool> OnProcess { get; set; }

        public void Enqueue(string key, BasicMessage message)
        {
            var startTask = false;
            List<BasicMessage> queue;
            lock (_queues)
            {
                if (!_queues.TryGetValue(key, out queue))
                {
                    queue = new List<BasicMessage>();
                    _queues.Add(key, queue);

                    startTask = true;
                }

                queue.Add(message);
            }

            if (startTask)
            {
                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            BasicMessage queuedMessage;
                            lock (_queues)
                            {
                                if (queue.Count == 0)
                                {
                                    _queues.Remove(key);
                                    return;
                                }

                                queuedMessage = queue[0];
                            }

                            try
                            {
                                if (OnProcess?.Invoke(queuedMessage) ?? false)
                                {
                                    queuedMessage.Channel?.BasicAck(queuedMessage.DeliveryTag, false);
                                }
                            }
                            catch
                            {
                                OnError?.Invoke(queuedMessage);
                                queuedMessage?.RegisterError?.Invoke();
                            }

                            lock (_queues)
                            {
                                queue.Remove(queuedMessage);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "An error occured while processing a message queue");
                        queue.Clear();
                        lock (_queues)
                        {
                            _queues.Remove(key);
                        }
                    }
                });
            }
        }
    }
}
