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
                .AddRabbitConfiguration("Configuration1", configuration.GetSection("RabbitConfigurations:Configuration1"))
                .AddRabbitConfiguration("Configuration2", configuration.GetSection("RabbitConfigurations:Configuration2"))
                .AddRabbitConfiguration("Configuration3", configuration.GetSection("RabbitConfigurations:Configuration3"))
                .AddPublisherFactory();

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<PublisherFactory>();
            var publisher = factory.GetPublisher("Configuration1");
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine($"Publishing: {i}");
                publisher.Publish("MyExchange", body: $"This is a test message - {DateTime.Now.ToLongDateString()}");
            }
            Console.WriteLine("done");

            publisher.Close();
        }
    }
}
