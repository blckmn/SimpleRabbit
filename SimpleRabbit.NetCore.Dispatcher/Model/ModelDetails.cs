using RabbitMQ.Client.Events;

namespace SimpleRabbit.NetCore.Dispatcher
{
    public class ModelDetails<T>
    {
        public T Message;
        public BasicDeliverEventArgs Args;
    }
}