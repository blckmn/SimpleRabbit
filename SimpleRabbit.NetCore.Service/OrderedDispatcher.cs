using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Service
{
    /// <summary>
    /// Dispatcher with ordering preservation, with the goal to maximize throughput.
    /// </summary>
    /// <typeparam name="T"> The type of processed message to handle</typeparam>
    public abstract class OrderedDispatcher<T> : IMessageHandler where T : IDispatchModel
    {
        protected OrderedDispatcher(ILogger<OrderedDispatcher<T>> logger)
        {
            _logger = logger;
        }

        private readonly Dictionary<string, Queue<T>> _queues = new Dictionary<string, Queue<T>>();
        private readonly object _semaphore = new object();
        protected readonly ILogger<OrderedDispatcher<T>> _logger;

        public abstract bool CanProcess(string tag);

        /// <summary>
        /// Process a basic message into the appropriate processing type
        /// </summary>
        /// <param name="message"> the basic delivered message</param>
        /// <returns></returns>
        /// <remarks></remarks>
        protected abstract T Get(BasicMessage message);

        protected abstract string GetKey(T model);

        /// <summary>
        /// Process a message
        /// </summary>
        /// <param name="model"> the payload representing a message</param>
        /// <returns> return true if this is to acknowledge immediately</returns>
        /// <remarks>
        /// 
        /// Throw an exception if this message is to be unacknowledged
        /// 
        /// </remarks>
        protected abstract bool ProcessMessage(T model);


        /// <summary>
        /// Recieve an Event, and queue it up
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Process(BasicMessage message)
        {
            var model = Get(message);
            model.BasicMessage = message;
            var key = GetKey(model);
            lock (_semaphore)
            {
                if (!_queues.TryGetValue(key, out var queue))
                {
                    //Start a new queue if not existing
                    queue = new Queue<T>();
                    _queues.Add(key, queue);
                    Task.Run(() => ProcessQueue(queue, key));
                }

                queue.Enqueue(model);

            }

            // Acknowledgement will be handled by Processing queue.
            return false;
        }

        private void ProcessQueue(Queue<T> queue, string key)
        {
            try
            {
                while (true)
                {
                    T details;
                    lock (_semaphore)
                    {
                        if (queue.Count == 0)
                        {
                            _queues.Remove(key);
                            return;
                        }

                        details = queue.Peek();
                    }

                    try
                    {
                        if (ProcessMessage(details))
                        {
                            details.BasicMessage?.Ack();
                        }
                        lock (_semaphore)
                        {
                            queue.Dequeue();
                        }
                    }
                    catch (Exception e)
                    {
                        // problem, what happens when its a drop message...? all messages effectively get dropped...
                        // In dropping messages, what happens when a new one of the same key comes in afterwards, the order could be broken.
                        // Maybe make it so that drop message is not an option/ or put a warning disclaimer.

                        _logger.LogError(e, "An error occured while trying to handle a message in a dispatcher processing queue");
                        // Create a static list to iterate over

                        // Check if this blocks, or has issues with getting added to.
                        foreach (var queuedMessage in queue)
                        {
                            queuedMessage?.BasicMessage?.ErrorAction?.Invoke();
                        }
                        queue.Clear();
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
