using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimpleRabbit.NetCore.Service
{
    public static class ServiceCollectionExtension
    {
        private static void AddRabbitServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<RabbitConfiguration>(config.GetSection("RabbitConfiguration"));
        }

        public static void AddSubscriberServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddRabbitServices(config);

            services.Configure<List<Subscriber>>(config.GetSection("Subscribers"));
            services.AddSingleton<IHostedService, SubscriberService>();
            services.AddTransient<IQueueService, QueueService>();
        }
    }
}
