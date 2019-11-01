using System;
using System.Collections.Generic;
using System.Linq;
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

            

            PublishService PublisherFactory(IServiceProvider service, string name,IConfigurationSection section)
            {
                var p = new PublishService(section.Get<RabbitConfiguration>());
                p.ConfigurationName = name;
                return p;
            }

            var services = new ServiceCollection();

            services
                .AddSingleton<IPublishService>(c => PublisherFactory(c, "Configuration1", configuration.GetSection("RabbitConfigurations:Configuration1")))
                .AddSingleton<IPublishService>(c => PublisherFactory(c, "Configuration2", configuration.GetSection("RabbitConfigurations:Configuration2")))
                .AddSingleton<IPublishService>(c => PublisherFactory(c, "Configuration3", configuration.GetSection("RabbitConfigurations:Configuration3")))
                .AddTransient(c => c.GetServices<IPublishService>().ToList());

            var provider = services.BuildServiceProvider();

            var publisher = provider.GetRequiredService<List<IPublishService>>().FirstOrDefault(p => p.ConfigurationName.Equals("Configuration1"));

            Console.Write($"Publishing: ");
            publisher.Publish("ExchangeName", body: $"This is a test message - {DateTime.Now.ToLongDateString()}");
            Console.WriteLine("done");

            publisher.Close();
        }
    }
}
