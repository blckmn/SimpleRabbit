using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleRabbit.NetCore
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddPublisherServices(this IServiceCollection services, IConfiguration config)
        {
            return services
                .Configure<RabbitConfiguration>(config.GetSection("RabbitConfiguration"))
                .AddTransient<IPublishService, PublishService>();
        }
    }
}
