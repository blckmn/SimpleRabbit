using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore.Service
{
    public interface IMessageHandler
    {
        bool CanProcess(string tag);
        bool Process(BasicDeliverEventArgs args);
    }
}
