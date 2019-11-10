using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleRabbit.NetCore
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddPublisherServices(this IServiceCollection services)
        {
            return services
                .AddTransient<IPublishService, PublishService>();
        }
        public static IServiceCollection AddRabbitConfiguration(this IServiceCollection services, IConfigurationSection config)
        {
            return services
                .Configure<RabbitConfiguration>(config)
                .AddSingleton(c => c.GetService<IOptions<RabbitConfiguration>>()?.Value);
        }
    }
}
