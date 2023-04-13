using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SimpleRabbit.NetCore.Tests.implementations;

namespace SimpleRabbit.NetCore.Tests;

/// <summary>
/// Testing message acknowledgement behaviour
/// </summary>
[TestFixture]
[NonParallelizable]
public class MessageAckTests : IntegrationFixture
{
    private int _processCount; 
    private static ILogger<QueueService> _nullLogger = NullLogger<QueueService>.Instance;
    private static QueueConfiguration _config = new()
    {
        QueueName = QueueName,
        // ConsumerTag is required
        ConsumerTag = "test"
    };
    
    [OneTimeSetUp]
    public void AddHandlerEvent()
    {
        handler.Handler = Process;
    }
    
    private Acknowledgement Process(BasicMessage args)
    {
        _processCount++;
        return args.Body switch {
            "ack" => Acknowledgement.Ack,
            "nackrequeue" => Acknowledgement.NackRequeue,
            "nackdeadletter" => Acknowledgement.NackDeadLetter,
            _ => Acknowledgement.Ignore
        };
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        basicService.ExposedChannel.QueuePurge(QueueName);
        basicService.ExposedChannel.QueuePurge("MyDeadQueue");
        handler.Handler -= Process;
    }

    [SetUp]
    public void Reset()
    {
        basicService.ExposedChannel.QueuePurge(QueueName);
        basicService.ExposedChannel.QueuePurge("MyDeadQueue");
        _processCount = 0;
    }
    
    [Test]
    public async Task Ack_ShouldBeRemovedFromQueue()
    {
        // Arrange
        var queue = new QueueService(ExposedRabbitService.validConfig, _nullLogger);
        queue.Start(_config, handler);
        
        var body = Encoding.UTF8.GetBytes("ack");
        basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, body);

        await Task.Delay(RoundTripWaitTime);

        // Act
        var messageCount = basicService.ExposedChannel.MessageCount(QueueName);

        // Assert
        _processCount.Should().Be(1);
        messageCount.Should().Be(0);
    }
    
    [Test]
    public async Task NackRequeue_ShouldBeRequeued()
    {
        // Arrange
        var queue = new QueueService(ExposedRabbitService.validConfig, _nullLogger);
        queue.Start(_config, handler);
        
        var body = Encoding.UTF8.GetBytes("nackrequeue");
        basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, body);
        
        await Task.Delay(RoundTripWaitTime);
        queue.Dispose();

        // Act
        var messageCount = basicService.ExposedChannel.MessageCount(QueueName);

        // Assert
        _processCount.Should().NotBe(0);
        messageCount.Should().Be(1);
    }
    
    [Test]
    public async Task NackDeadLetter_ShouldBeRemovedFromQueue()
    {
        // Arrange
        var queue = new QueueService(ExposedRabbitService.validConfig, _nullLogger);
        queue.Start(_config, handler);
        
        var body = Encoding.UTF8.GetBytes("nackdeadletter");
        basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, body);

        await Task.Delay(RoundTripWaitTime);

        // Act
        var count = basicService.ExposedChannel.MessageCount(QueueName);
        var dqlCount = basicService.ExposedChannel.MessageCount("MyDeadQueue");

        // Assert
        count.Should().Be(0);
        dqlCount.Should().Be(1);
    }
    
    [Test]
    public async Task Ignore_ShouldRemainInQueue()
    {
        // Arrange
        var queue = new QueueService(ExposedRabbitService.validConfig, _nullLogger);
        queue.Start(_config, handler);
        
        var body = Encoding.UTF8.GetBytes("ignore");
        basicService.ExposedChannel.BasicPublish(ExchangeName, "", true, null, body);
        
        await Task.Delay(RoundTripWaitTime);
        queue.Dispose();
        await Task.Delay(RoundTripWaitTime);

        // Act
        var messageCount = basicService.ExposedChannel.MessageCount(QueueName);

        // Assert
        _processCount.Should().Be(1);
        messageCount.Should().Be(1);
    }
}