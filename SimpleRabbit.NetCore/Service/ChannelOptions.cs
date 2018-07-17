using System;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore
{
    public interface IChannelOptions
    {
        void SetChannelOptions(IModel model, Action onError);
    }
}