using Microsoft.Extensions.DependencyInjection;

namespace SimpleRabbit.NetCore
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddPublisherServices(this IServiceCollection services)
        {
            return services
                .AddTransient<IPublishService, PublishService>();
        }
    }
}
