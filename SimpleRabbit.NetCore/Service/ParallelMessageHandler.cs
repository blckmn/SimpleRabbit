using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleRabbit.NetCore
{
    /// <summary>
    /// A task queue based ordered dispatcher. Retrieves messages in batch synchronously, and handles them in parallel. Level of parallism is dependent on the prefetch setting in Rabbit.
    /// </summary>
    /// <typeparam name="TKey">The key to use for ordering</typeparam>
    /// <typeparam name="TValue">The value to work on</typeparam>
    public abstract class ParallelMessageHandler<TKey, TValue> : IMessageHandler
    {
        private readonly ILogger<ParallelMessageHandler<TKey, TValue>> _logger;

        private readonly Dictionary<TKey, Task> _tasks;
        private readonly object _lock = new object();

        public class DeserializedMessage<TKey, TValue>
        {
            public TKey Key { get; private set; }
            public TValue Value { get; private set; }
            public Acknowledgement? Ack { get; private set; }

            public static DeserializedMessage<TKey, TValue> Success(TKey key, TValue value)
            {
                return new DeserializedMessage<TKey, TValue>
                {
                    Key = key,
                    Value = value,
                };
            }
            
            public static DeserializedMessage<TKey, TValue> Fail(Acknowledgement ack)
            {
                return new DeserializedMessage<TKey, TValue>
                {
                    Ack = ack
                };
            }
        }
        
        protected ParallelMessageHandler(ILogger<ParallelMessageHandler<TKey, TValue>> logger)
        {
            _logger = logger;
            _tasks = new Dictionary<TKey, Task>();
        }

        protected ParallelMessageHandler(ILogger<ParallelMessageHandler<TKey, TValue>> logger,
            Dictionary<TKey, Task> dictionary)
        {
            _logger = logger;
            _tasks = dictionary;
        }

        /// <summary>
        /// This method is required for IMessageHandler implementation.
        /// </summary>
        /// <returns></returns>
        public abstract bool CanProcess(string tag);

        /// <summary>
        /// This method is run sequentially. The number of tasks will be dependent on the prefetch setting in Rabbit.
        /// </summary>
        /// <param name="message"></param>
        public Acknowledgement Process(BasicMessage message)
        {
            try
            {
                if (!TryDeserializeMessage(message, out var value))
                {
                    _logger.LogInformation("{ValueAck} message {PropertiesMessageId} as it failed to deserialize -> {MessageBody},", 
                        value.Ack,
                        message.Properties?.MessageId, 
                        message.Body);
                    return value.Ack;
                }
                
                _logger.LogDebug($"Processing message for {value.Key}");
                
                if (key == null)
                {
                    // Acking so the message gets removed from the queue
                    return Acknowledgement.Ack;
                }

                // Enforce thread safety when manipulating the dictionary of running tasks
                lock (_lock)
                {
                    CleanUpTasks();

                    if (!_tasks.TryGetValue(key, out var task))
                    {
                        task = Task.CompletedTask;
                    }

                    // save the task at the end of the queues.
                    var tailTask = ContinueTaskQueue(task, message, key, item);
                    _tasks[key] = tailTask; // add completed task to the to be used.
                }
                
                // Ignoring as we are handling the acking ourselves in parallel
                return Acknowledgement.Manual;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error Processing message {message.Body}, {e.Message}");
                throw;
            }
        }
        
        protected abstract bool TryDeserializeMessage(BasicMessage msg, [MaybeNullWhen(false)] out DeserializedMessage<TKey, TValue> value);
        
        private void CleanUpTasks()
        {
            var completed = _tasks
                .Where(t => t.Value?.IsCompleted ?? true)
                .Select(t => t.Key)
                .ToArray();
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
                    message.HandleAck(Acknowledgement.NackRequeue);
                    throw new Exception($"Processing chain aborted for {key}");
                }

                try
                {
                    var acknowledgement = await ProcessAsync(item);
                    message.HandleAck(acknowledgement);
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

        protected abstract Task<Acknowledgement> ProcessAsync(TValue item);
    }
    
}
