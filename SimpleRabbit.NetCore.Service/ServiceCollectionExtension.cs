using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace SimpleRabbit.NetCore.Service
{
    public static class ServiceCollectionExtension
    {

        /// <summary>
        /// Register a <see cref="IHostedService"/> that will call <see cref="IMessageHandler"/> to process messages
        /// </summary>
        /// <param name="services"> the <see cref="IServiceCollection"/> to register the subscriber service to </param>
        /// <returns>the <see cref="IServiceCollection"/></returns>
        /// <remarks>
        /// Relies On <see cref="QueueConfiguration"/> to be configured before this method, 
        /// which can be done via <see cref="AddSubscriberConfiguration(IServiceCollection, IConfigurationSection)"/>
        /// </remarks>
        public static IServiceCollection AddSubscriberServices(this IServiceCollection services)
        {
            return services
                .AddHostedService<QueueFactory>();
        }

        /// <summary>
        /// Add a <see cref="IMessageHandler"/> that can handle messages
        /// </summary>
        /// <typeparam name="T"> the implementation to add</typeparam>
        /// <param name="services">the <see cref="IServiceCollection"/></param>
        /// <returns></returns>
        public static IServiceCollection AddSubscriberHandler<T>(this IServiceCollection services) where T : class, IMessageHandler
        {
            return services.AddSingleton<IMessageHandler, T>();
        }
        /// <summary>
        /// Add a <see cref="IMessageHandlerAsync"/> that can handle messages
        /// </summary>
        /// <typeparam name="T"> the implementation to add</typeparam>
        /// <param name="services">the <see cref="IServiceCollection"/></param>
        /// <returns></returns>
        public static IServiceCollection AddAsyncSubscriberHandler<T>(this IServiceCollection services) where T : class, IMessageHandlerAsync
        {
            return services.AddSingleton<IMessageHandlerAsync, T>();
        }

        /// <summary>
        /// configure the a named list of <see cref="QueueConfiguration"/> to <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">the <see cref="IServiceCollection"/> to configure</param>
        /// <param name="config">the configuration section to use</param>
        /// <returns>the <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddSubscriberConfiguration(this IServiceCollection services, string name, IConfigurationSection config)
        {
            // This will only be instaniated and NEVER updated
            services.Configure<List<Subscribers>>(s => s.Add(new Subscribers { Name = name }));

            return services.Configure<List<QueueConfiguration>>(name, config);
        }

        /// <summary>
        /// configure the default list of <see cref="QueueConfiguration" /> to <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services"> the <see cref="IServiceCollection"/> to configure</param>
        /// <param name="config">the configuration esction to use</param>
        /// <returns></returns>
        public static IServiceCollection AddSubscriberConfiguration(this IServiceCollection services, IConfigurationSection config)
        {
            // This will only be instaniated and NEVER updated
            services.Configure<List<Subscribers>>(s => s.Add(new Subscribers { Name = Options.DefaultName }));

            return services.Configure<List<QueueConfiguration>>(Options.DefaultName, config);
        }


    }
}
