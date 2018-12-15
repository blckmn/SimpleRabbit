using Microsoft.Extensions.DependencyInjection;

namespace SimpleRabbit.NetCore.Dispatcher
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDispatcherServices(this IServiceCollection services)
        {
            return services
                .AddTransient<IMessageDispatcher, MessageDispatcher>();
        }
    }
}
