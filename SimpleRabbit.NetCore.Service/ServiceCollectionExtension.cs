using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimpleRabbit.NetCore.Service
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddSubscriberServices(this IServiceCollection services, IConfiguration config)
        {
            return services
                .AddSingleton<IHostedService, SubscriberService>()
                .AddTransient<IQueueService, QueueService>();
        }

        public static IServiceCollection AddSubscriberConfiguration(this IServiceCollection services, IConfiguration config)
        {
            return services
                .Configure<List<SubscriberConfiguration>>(config.GetSection("Subscribers"));
        }
    }
}
