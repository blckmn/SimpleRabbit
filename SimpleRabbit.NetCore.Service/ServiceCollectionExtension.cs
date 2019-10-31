using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    }
}
