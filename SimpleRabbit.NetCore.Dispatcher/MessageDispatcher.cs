using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Dispatcher
{ 
    public interface IMessageDispatcher
    {
        void Enqueue(string key, BasicMessage message);
        void Init(Func<BasicMessage, bool> onProcess, Action<Exception, BasicMessage> onError = null);
    }

    internal class KeyedQueue 
    {
        public KeyedQueue(string key) 
        {
            Key = key;
        }
        
        public string Key { get; }
        public Task Task { get; set; }
        public readonly List<BasicMessage> Messages = new List<BasicMessage>();
    }

    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly Dictionary<string, KeyedQueue> _queues = new Dictionary<string, KeyedQueue>();

        public void Init(Func<BasicMessage, bool> onProcess, Action<Exception, BasicMessage> onError = null)
        {
            OnProcess = onProcess;
            OnError = onError;
        }

        public Action<Exception, BasicMessage> OnError { get; set; }
        public Func<BasicMessage, bool> OnProcess { get; set; }

        public void Enqueue(string key, BasicMessage message)
        {
            var startTask = false;
            KeyedQueue queue;
            lock (_queues)
            {
                if (!_queues.TryGetValue(key, out queue))
                {
                    queue = new KeyedQueue(key);
                    _queues.Add(key, queue);

                    startTask = true;
                }

                queue.Messages.Add(message);
            }

            if (startTask)
            {
                queue.Task = Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            BasicMessage queuedMessage;
                            lock (_queues)
                            {
                                if (queue.Messages.Count == 0)
                                {
                                    _queues.Remove(key);
                                    return;
                                }

                                queuedMessage = queue.Messages[0];
                            }

                            try
                            {
                                if (OnProcess?.Invoke(queuedMessage) ?? false)
                                {
                                    queuedMessage.Channel?.BasicAck(queuedMessage.DeliveryTag, false);
                                }
                            }
                            catch (Exception e)
                            {
                                OnError?.Invoke(e, queuedMessage);
                                queuedMessage?.RegisterError?.Invoke();
                            }

                            lock (_queues)
                            {
                                queue.Messages.Remove(queuedMessage);
                            }
                        }
                    }
                    catch
                    {
                        /* serious problem if we land here */
                        queue.Messages.Clear();
                        lock (_queues)
                        {
                            _queues.Remove(key);
                        }

                        throw;
                    }
                });
            }
        }
    }
}
