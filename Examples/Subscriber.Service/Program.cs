using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Service.Service;
using System;
using System.Threading.Tasks;

namespace Subscriber.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var builder = new HostBuilder()
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddJsonFile("appsettings.json");
                        hostingContext.HostingEnvironment.EnvironmentName = "Development";
                    })
                    .ConfigureServices((context, services) =>
                    {
                        var config = context.Configuration;
                        services
                            //.AddSubscriberHandler<MessageProcessor>()
                            .AddAsyncSubscriberHandler<MessageProcessorAsync>()
                            .AddRabbitConfiguration(config.GetSection("RabbitConfiguration"))
                            .AddSubscriberConfiguration(config.GetSection("Subscribers"))
                            .AddSubscriberServices();
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
