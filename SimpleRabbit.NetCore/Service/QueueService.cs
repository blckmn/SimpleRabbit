using System;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore
{
    public interface IQueueService
    {
	    void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1);
        void Start(Subscriber subscriber, IMessageHandler handler);
	    void Stop();
    }

    public class QueueService : IQueueService, IDisposable
    {
        private readonly ILogger<QueueService> _logger;
        private readonly ConnectionFactory _factory;

	    private readonly Timer _timer;
		public QueueService(IOptions<RabbitConfiguration> options, ILogger<QueueService> logger)
        {
            _logger = logger;
            var config = options.Value;

	        _timer = new Timer
	        {
		        AutoReset = false,
	        };
	        _timer.Elapsed += (sender, args) =>
	        {
		        Start();
		        _timer.Stop();
	        };

			_factory = new ConnectionFactory
            {
                Uri = config.Uri,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                RequestedHeartbeat = 5
            };
		}

        private const int MaxRetryInterval = 120;
        private void RestartIn(TimeSpan waitInterval)
	    {
	        _retryCount++;
	        var interval = waitInterval.TotalSeconds * (_autoBackOff ? _retryCount : 1) % MaxRetryInterval;
            
            _timer.Interval = interval * 1000;
            _logger.LogInformation($" -> restarting connection in {interval} seconds ({_retryCount}).");
		    _timer.Start();
		    Stop();
	    }

		private IConnection _connection;
        private IConnection Connection => _connection ?? (_connection = _factory.CreateConnection());

        private IModel _channel;
        private IModel Channel => _channel ?? (_channel = Connection.CreateModel());

	    private string _queue;
	    private string _tag;
	    private IMessageHandler _handler;
        private ushort _prefetch;
        private int _retryInterval;
        private bool _autoBackOff;
        private int _retryCount;

        public void Start(string queue, string tag, IMessageHandler handler, ushort prefetch = 1)
        {
	        Stop();
            _prefetch = prefetch;
	        _queue = queue;
	        _tag = tag;
	        _handler = handler;
			Start();
        }

        public void Start(Subscriber subscriber, IMessageHandler handler)
        {
            Stop();
            _prefetch = subscriber.PrefetchCount ?? 1;
            _queue = subscriber.QueueName;
            _tag = subscriber.ConsumerTag;
            _retryInterval = subscriber.RetryInterval;
            _autoBackOff = subscriber.AutoBackOff;
            _handler = handler;
            Start();
        }

        private void Start()
	    {
	        try
	        {
	            var consumer = new EventingBasicConsumer(Channel);
	            consumer.Received += (model, ea) =>
	            {
	                var channel = consumer.Model;
	                try
	                {
	                    _handler?.Process(ea);
	                    channel.BasicAck(ea.DeliveryTag, false);
	                    _retryCount = 0;
	                }
                    catch (Exception ex)
	                {
	                    // error processing message
	                    _logger.LogError(ex, $"{ex.Message} -> {ea.DeliveryTag}: {ea.BasicProperties.MessageId}");
	                    RestartIn(TimeSpan.FromSeconds(_retryInterval));
	                }
	            };
                Channel.BasicQos(0, _prefetch, false);
	            Channel.BasicConsume(_queue, false, _tag, consumer);
	        }
	        catch (Exception e)
	        {
                _logger.LogError(e, e.Message);
                RestartIn(TimeSpan.FromSeconds(_retryInterval));
	        }
	    }

	    public void Stop()
        {
            _channel?.Dispose();
            _channel = null;

            _connection?.Dispose();
            _connection = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
