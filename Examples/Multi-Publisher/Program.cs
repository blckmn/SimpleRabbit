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

            

            NamedService<IPublishService> PublisherFactory(IServiceProvider service, string name,IConfigurationSection section)
            {
                var p = new NamedService<IPublishService>();
                p.Name = name;
                p.Service = new PublishService(section.Get<RabbitConfiguration>());
                return p;
            }

            var services = new ServiceCollection();

            services
                .AddSingleton(c => PublisherFactory(c, "Configuration1", configuration.GetSection("RabbitConfigurations:Configuration1")))
                .AddSingleton(c => PublisherFactory(c, "Configuration2", configuration.GetSection("RabbitConfigurations:Configuration2")))
                .AddSingleton(c => PublisherFactory(c, "Configuration3", configuration.GetSection("RabbitConfigurations:Configuration3")))
                .AddTransient(c => c.GetServices<NamedService<IPublishService>>().ToList());

            var provider = services.BuildServiceProvider();

            var publisher = provider.GetRequiredService<List<NamedService<IPublishService>>>().FirstOrDefault(p => p.Name.Equals("Configuration1"));

            Console.Write($"Publishing: ");
            publisher.Service.Publish("Example", body: $"This is a test message - {DateTime.Now.ToLongDateString()}");
            Console.WriteLine("done");

            publisher.Service.Close();
        }
    }
}
