using RabbitMQ.Client;
using System.Collections.Generic;

namespace SimpleRabbit.NetCore.Tests.implementations
{

    public class ExposedRabbitService : BasicRabbitService
    {
        public static RabbitConfiguration validConfig = new RabbitConfiguration
        {
            Username = "guest",
            Password = "guest",
            Hostnames = new List<string> { "localhost" },
        };

        public ExposedRabbitService(RabbitConfiguration config) : base(config)
        {

        }

        public ConnectionFactory ExposedFactory { get => Factory; }
        public IConnection ExposedConnection { get => Connection; }
        public IModel ExposedChannel { get => Channel; }
    }
}
