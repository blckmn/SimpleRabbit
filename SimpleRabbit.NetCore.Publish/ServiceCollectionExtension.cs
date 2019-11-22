using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleRabbit.NetCore.Publisher;

namespace SimpleRabbit.NetCore
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Add a singular publisher <see cref="IPublishService"/> instance registered with default configuration to <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services"> the collection to add this to</param>
        /// <returns> the <see cref="IServiceCollection"/></returns>
        /// <remarks>
        /// Will add a singleton <see cref="RabbitConfiguration"/> to be used by the <see cref="IPublishService"/>
        /// </remarks>
        public static IServiceCollection AddPublisherServices(this IServiceCollection services)
        {
            return services
                .AddTransient<IPublishService, PublishService>()
                .AddSingleton(c => c.GetService<IOptions<RabbitConfiguration>>()?.Value);
        }

        /// <summary>
        /// Add a singular publisher <see cref="IPublishService"/> instance registered with default configuration to <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services"> the collection to add this to</param>
        /// <returns> the <see cref="IServiceCollection"/></returns>
        /// <remarks>
        /// Will add a singleton <see cref="RabbitConfiguration"/> to be used by the <see cref="IPublishService"/>
        /// </remarks>
        public static IServiceCollection AddPublisherFactory(this IServiceCollection services)
        {
            return services.AddSingleton<PublisherFactory>();
        }
    }
}
