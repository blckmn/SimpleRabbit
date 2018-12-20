using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Service.Service;
using Environment = System.Environment;

namespace Subscriber.Service
{
    public class Program
    {
        private static string HostingEnvironment => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? 
            EnvironmentName.Development :
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        public static async Task Main(string[] args)
        {
            try
            {
                var builder = new HostBuilder()
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddJsonFile($"appsettings.{HostingEnvironment}.json");

                        hostingContext.HostingEnvironment.EnvironmentName = HostingEnvironment;
                        config.AddEnvironmentVariables();

                        if (args != null)
                        {
                            config.AddCommandLine(args);
                        }
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddSingleton<ILoggerFactory, LoggerFactory>()
                            .AddSingleton<ILogger>(ctx => ctx.GetService<ILogger<HostBuilder>>())
                            .AddSubscriberServices(context.Configuration);

                        services
                            .AddSingleton<IMessageHandler, MessageProcessorDispatch>()
                            .AddSingleton<IMessageHandler, MessageProcessor>();
                    })
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                    });

                await builder.RunConsoleAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
