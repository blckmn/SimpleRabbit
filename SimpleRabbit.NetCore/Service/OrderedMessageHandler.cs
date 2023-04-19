using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleRabbit.NetCore
{
    /// <summary>
    /// A task queue based ordered dispatcher. Processes messages based on provided key in FIFO.
    /// The number of tasks is dependent on the prefetch setting in Rabbit.
    /// </summary>
    /// <typeparam name="TKey">The key to use for ordering</typeparam>
    /// <typeparam name="TValue">The value to work on</typeparam>
    public abstract class OrderedMessageHandler<TKey, TValue> : IMessageHandler
    {
        private readonly ILogger<OrderedMessageHandler<TKey, TValue>> _logger;

        private readonly Dictionary<TKey, Task> _tasks;
        private readonly object _lock = new object();

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
        
        public abstract bool CanProcess(string tag);
        
        /// <summary>
        /// Parse the message into its key and value.
        /// </summary>
        /// <param name="message">A message containing headers and the raw data.</param>
        /// <returns><see cref="ParseResult"/> object.</returns>
        protected abstract ParseResult Parse(BasicMessage message);
        
        /// <summary>
        /// Process the value asynchronously.
        /// </summary>
        /// <param name="value">The value to work on.</param>
        /// <returns><see cref="Acknowledgement"/> enum denoting how the message should be acknowledged.</returns>
        protected abstract Task<Acknowledgement> ProcessAsync(TValue value);
        
        public Task<Acknowledgement> Process(BasicMessage message)
        {
            try
            {
                var parseResult = Parse(message);
                if (!parseResult.IsSuccessful)
                {
                    _logger.LogInformation("{ValueAck} message {PropertiesMessageId} as it failed to deserialize -> {MessageBody},", 
                        parseResult.Ack,
                        message.Properties?.MessageId, 
                        message.Body);
                    
                    return Task.FromResult(parseResult.Ack.Value);
                }
                
                _logger.LogDebug($"Processing message for {parseResult.Key}");

                // Enforce thread safety when manipulating the dictionary of running tasks
                lock (_lock)
                {
                    CleanUpTasks();

                    if (!_tasks.TryGetValue(parseResult.Key, out var task))
                    {
                        task = Task.CompletedTask;
                    }

                    // save the task at the end of the queues.
                    var tailTask = ContinueTaskQueue(task, message, parseResult.Key, parseResult.Value);
                    _tasks[parseResult.Key] = tailTask; // add completed task to the to be used.
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

        /// <summary>
        /// Result of parsing the key and value of a message.
        /// </summary>
        public class ParseResult
        {   
            /// <summary>
            /// Message key. This is used for ordering.
            /// </summary>
            public TKey Key { get; private set; }
            
            /// <summary>
            /// Message value to be processed.
            /// </summary>
            public TValue Value { get; private set; }
            
            /// <summary>
            /// Null if successful, otherwise used to specify message acknowledgement. 
            /// </summary>
            public Acknowledgement? Ack { get; private set; }
            
            /// <summary>
            /// True if parse was successful, false otherwise.
            /// </summary>
            public bool IsSuccessful { get; private set; }
            
            /// <summary>
            /// Creates a success result.
            /// </summary>
            /// <param name="key">Message key.</param>
            /// <param name="value">Message value.</param>
            /// <returns><see cref="ParseResult"/> object with success set to true.</returns>
            public static ParseResult Success(TKey key, TValue value)
            {
                return new ParseResult
                {
                    Key = key,
                    Value = value,
                    IsSuccessful = true
                };
            }
            
            /// <summary>
            /// Creates a failure result.
            /// </summary>
            /// <param name="ack">Message <see cref="Acknowledgement"/> to send for erroneous message.</param>
            /// <returns></returns>
            public static ParseResult Fail(Acknowledgement ack)
            {
                return new ParseResult
                {
                    Ack = ack,
                    IsSuccessful = false
                };
            }
        }
    }
}
