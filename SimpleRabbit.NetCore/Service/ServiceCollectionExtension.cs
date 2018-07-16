using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleRabbit.NetCore.Model;

namespace SimpleRabbit.NetCore.Service
{
    public static class ServiceCollectionExtension
    {
        private static void AddRabbitServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<RabbitConfiguration>(config.GetSection("RabbitConfiguration"));
        }

        public static void AddPublisherServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddRabbitServices(config);
            services.AddTransient<IPublishService, PublishService>();
        }
    }
}
