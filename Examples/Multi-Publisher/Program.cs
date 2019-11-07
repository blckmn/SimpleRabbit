using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Publisher;

namespace Publisher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .Build();

            var services = new ServiceCollection();

            services
                .Configure<RabbitConfiguration>("Configuration1",configuration.GetSection("RabbitConfigurations:Configuration1"))
                .Configure<RabbitConfiguration>("Configuration2", configuration.GetSection("RabbitConfigurations:Configuration2"))
                .Configure<RabbitConfiguration>("Configuration3", configuration.GetSection("RabbitConfigurations:Configuration3"))
                .AddSingleton<PublisherFactory>();

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<PublisherFactory>();
            var publisher = factory.GetPublisher("Configuration1");
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine($"Publishing: {i}");
                publisher.Publish("Example", body: $"This is a test message - {DateTime.Now.ToLongDateString()}");
            }
            Console.WriteLine("done");

            publisher.Close();
        }
    }
}
