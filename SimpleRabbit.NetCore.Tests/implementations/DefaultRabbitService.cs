using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleRabbit.NetCore.Tests.implementations
{

    public class DefaultRabbitService : BasicRabbitService
    {
        public static RabbitConfiguration validConfig = new RabbitConfiguration
        {
            Username = "guest",
            Password = "guest",
            Hostnames = new List<string> { "localhost" },
        };

        public DefaultRabbitService(RabbitConfiguration config) : base(config)
        {

        }

        public ConnectionFactory ExposedFactory { get => Factory; }
        public IConnection ExposedConnection { get => Connection; }
        public IModel ExposedChannel { get => Channel; }
    }
}
