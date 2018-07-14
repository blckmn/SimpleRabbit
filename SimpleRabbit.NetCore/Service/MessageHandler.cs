using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore
{
    public interface IMessageHandler
    {
        bool CanProcess(string tag);
        bool Process(BasicDeliverEventArgs args);
    }
}
