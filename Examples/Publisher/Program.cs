using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                .AddLogging(logging =>
                {
                    logging.AddConfiguration(configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .AddPublisherServices()
                .BuildServiceProvider();

            var publisher = provider.GetService<IPublishService>();

            for (var index = 0; index < 100; index++)
            {
                Console.Write($"Publishing {index}: ");
                publisher.Publish("", route: "robin-test", body: $"This is a test message - {index} - {DateTime.Now.ToLongDateString()}");
                Console.WriteLine("done");
            }

            Console.WriteLine("Paused");
            Console.ReadKey();

            for (var index = 0; index < 100; index++)
            {
                Console.Write($"Publishing {index}: ");
                publisher.Publish("", route: "robin-test", body: $"This is a test message - {index} - {DateTime.Now.ToLongDateString()}");
                Console.WriteLine("done");
            }

            Console.WriteLine("Paused");
            Console.ReadKey();

            publisher.Close();
        }
    }
}
