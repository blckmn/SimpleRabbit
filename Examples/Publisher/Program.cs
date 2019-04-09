using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleRabbit.NetCore;

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
            services.AddPublisherServices(configuration);

            var provider = services.BuildServiceProvider();

            var publisher = provider.GetService<IPublishService>();

            for (var index = 0; index < 1000; index++)
            {
                Console.Write($"Publishing {index} : ");
                publisher.Publish("Example", body: $"This is a test message - {index} - {DateTime.Now.ToLongDateString()}");
                Console.WriteLine("done");
            }

            publisher.Close();
        }
    }
}
