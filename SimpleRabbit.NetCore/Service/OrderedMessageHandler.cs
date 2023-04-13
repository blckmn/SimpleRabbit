using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleRabbit.NetCore
{
    /// <summary>
    /// A task queue based ordered dispatcher. Processes messages based on provided key in FIFO.
    /// </summary>
    /// <typeparam name="TKey">The key to use for ordering</typeparam>
    /// <typeparam name="TValue">The value to work on</typeparam>
    public abstract class OrderedMessageHandler<TKey, TValue> : IMessageHandler
    {
        private readonly ILogger<OrderedMessageHandler<TKey, TValue>> _logger;

        private readonly Dictionary<TKey, Task> _tasks;
        private readonly object _lock = new object();

        public class DeserializedMessage
        {
            public TKey Key { get; private set; }
            public TValue Value { get; private set; }
            public Acknowledgement? Ack { get; private set; }
            public bool Successful { get; private set; }

            public static DeserializedMessage Success(TKey key, TValue value)
            {
                return new DeserializedMessage
                {
                    Key = key,
                    Value = value,
                    Successful = true
                };
            }
            
            public static DeserializedMessage Fail(Acknowledgement ack)
            {
                return new DeserializedMessage
                {
                    Ack = ack,
                    Successful = false
                };
            }
        }
        
        protected OrderedMessageHandler(ILogger<OrderedMessageHandler<TKey, TValue>> logger)
        {
            _logger = logger;
            _tasks = new Dictionary<TKey, Task>();
        }

        protected OrderedMessageHandler(ILogger<OrderedMessageHandler<TKey, TValue>> logger,
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
        public Task<Acknowledgement> Process(BasicMessage message)
        {
            try
            {
                var deserializedMessage = TryDeserializeMessage(message);
                if (!deserializedMessage.Successful)
                {
                    _logger.LogInformation("{ValueAck} message {PropertiesMessageId} as it failed to deserialize -> {MessageBody},", 
                        deserializedMessage.Ack,
                        message.Properties?.MessageId, 
                        message.Body);
                    
                    return Task.FromResult(deserializedMessage.Ack.Value);
                }
                
                _logger.LogDebug($"Processing message for {deserializedMessage.Key}");

                // Enforce thread safety when manipulating the dictionary of running tasks
                lock (_lock)
                {
                    CleanUpTasks();

                    if (!_tasks.TryGetValue(deserializedMessage.Key, out var task))
                    {
                        task = Task.CompletedTask;
                    }

                    // save the task at the end of the queues.
                    var tailTask = ContinueTaskQueue(task, message, deserializedMessage.Key, deserializedMessage.Value);
                    _tasks[deserializedMessage.Key] = tailTask; // add completed task to the to be used.
                }
                
                // Ignoring as we are handling the acking ourselves in parallel
                return Task.FromResult(Acknowledgement.Manual);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error Processing message {message.Body}, {e.Message}");
                throw;
            }
        }
        
        protected abstract DeserializedMessage TryDeserializeMessage(BasicMessage msg);
        // protected abstract bool TryDeserializeMessage(BasicMessage msg, [MaybeNullWhen(false)] out DeserializedMessage value);
        
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
