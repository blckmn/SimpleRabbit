using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleRabbit.NetCore;
using System;

namespace Publisher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .Build();

            var provider = new ServiceCollection()
                .AddRabbitConfiguration(configuration.GetSection("RabbitConfiguration"))
                .AddPublisherServices()
                .BuildServiceProvider();

            var publisher = provider.GetService<IPublishService>();

            for (var index = 0; index < 1000; index++)
            {
                Console.Write($"Publishing {index} : ");
                publisher.Publish("MyFeed.Api", body: $"This is a test message - {index} - {DateTime.Now.ToLongDateString()}");
                Console.WriteLine("done");
            }

            publisher.Close();
        }
    }
}
