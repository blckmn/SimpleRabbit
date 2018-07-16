using System;
using RabbitMQ.Client;

namespace SimpleRabbit.NetCore.Service
{
    public interface IChannelOptions
    {
        void SetChannelOptions(IModel model, Action onError);
    }
}