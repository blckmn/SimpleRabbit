using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SimpleRabbit.NetCore.Service
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddSubscriberServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IHostedService, SubscriberService>()
                .AddTransient<IQueueService, QueueService>();
        }

        public static IServiceCollection AddSubscriberConfiguration(this IServiceCollection services, IConfigurationSection config)
        {
            return services
                .Configure<List<SubscriberConfiguration>>(config)
                .AddSingleton((provider) =>
                {
                    return provider.GetService<IOptions<List<SubscriberConfiguration>>>().Value;
                }); ;
        }
    }
}
