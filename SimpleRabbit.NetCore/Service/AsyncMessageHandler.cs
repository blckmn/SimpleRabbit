using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleRabbit.NetCore
{
    /// <summary>
    ///     A task queue based ordered dispatcher
    /// </summary>
    /// <typeparam name="TKey">The key to use for ordering</typeparam>
    /// <typeparam name="TValue">The value to work on</typeparam>
    public abstract class AsyncMessageHandler<TKey, TValue> : IMessageHandler
    {
        private readonly ILogger<AsyncMessageHandler<TKey, TValue>> _logger;

        private readonly Dictionary<TKey, Task> _tasks;

        protected AsyncMessageHandler(ILogger<AsyncMessageHandler<TKey, TValue>> logger)
        {
            _logger = logger;
            _tasks = new Dictionary<TKey, Task>();
        }

        protected AsyncMessageHandler(ILogger<AsyncMessageHandler<TKey, TValue>> logger,
            Dictionary<TKey, Task> dictionary)
        {
            _logger = logger;
            _tasks = dictionary;
        }

        /// <summary>
        ///     This method is required for IMessageHandler implementation.
        /// </summary>
        /// <returns></returns>
        public abstract bool CanProcess(string tag);

        /// <summary>
        ///     This method is run sequentially. The number of tasks will be dependent on the prefetch setting in Rabbit.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Process(BasicMessage message)
        {
            CleanUpTasks();

            try
            {
                var item = Get(message);
                var key = GetKey(item);

                _logger.LogDebug($"Processing message for {key}");
                if (key == null)
                {
                    _logger.LogInformation($"Message ignored {message.Properties?.MessageId} -> {message.Body}, no key");
                    return true;
                }

                if (!_tasks.TryGetValue(key, out var task))
                {
                    task = Task.CompletedTask;
                }
                
                // save the task at the end of the queues.
                var tailTask = ContinueTaskQueue(task, message, key, item);
                _tasks[key] = tailTask; // add completed task to the to be used.
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error Processing message {message.Body}, {e.Message}");
                throw;
            }
        }

        /// <summary>
        ///     Must be provided to decompose the message to a TValue e.g. perform any deserialisation or object creation.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>The decomposed message</returns>
        protected abstract TValue Get(BasicMessage message);

        /// <summary>
        ///     Must be provided to extract the Key from the (decomposed) item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The decomposed message</returns>
        protected abstract TKey GetKey(TValue item);

        private void CleanUpTasks()
        {
            var completed = _tasks.Where(t => t.Value?.IsCompleted ?? true).Select(t => t.Key);
            foreach (var key in completed)
            {
                _tasks.Remove(key);
            }
        }

        private Task ContinueTaskQueue(Task task, BasicMessage message, TKey key, TValue item)
        {
            return task.ContinueWith(async t =>
            {
                if (!t.IsCompletedSuccessfully)
                {
                    message.Nack();
                    throw new Exception($"Processing chain aborted for {key}");
                }

                try
                {
                    await ProcessAsync(item);
                    message.Ack();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Couldn't process: {e.Message} key: {key} tag: ({message.DeliveryTag})");
                    if (e is AggregateException agg)
                    {
                        foreach (var ex in agg.InnerExceptions)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                
                    message.ErrorAction();
                    throw;
                }
            }).Unwrap();
        }

        protected abstract Task ProcessAsync(TValue item);
    }
}
