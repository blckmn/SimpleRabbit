using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleRabbit.NetCore
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// configure the default <see cref="RabbitConfiguration"/> to <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services"> the collection to add this to</param>
        /// <param name="config">the section of the configuration to use</param>
        /// <returns>the <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddRabbitConfiguration(this IServiceCollection services, IConfigurationSection config)
        {
            return services.Configure<RabbitConfiguration>(config);
        }

        /// <summary>
        /// Add a named <see cref="RabbitConfiguration"/> to <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">the collection to add this to</param>
        /// <param name="name">the name or key this is accessible by</param>
        /// <param name="config">the section of the configuration to use</param>
        /// <returns>the <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddRabbitConfiguration(this IServiceCollection services, string name, IConfigurationSection config)
        {
            return services.Configure<RabbitConfiguration>(name, config);
        }
    }
}
