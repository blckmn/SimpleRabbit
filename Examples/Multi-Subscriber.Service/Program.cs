using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;
using Subscriber.Service.Service;

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
                        config.AddJsonFile("appsettings.json",false,reloadOnChange:true);
                        hostingContext.HostingEnvironment.EnvironmentName = "Development";
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddSingleton<IMessageHandler,MessageProcessor>()
                            .AddSubscriberConfiguration(context.Configuration.GetSection("RabbitConfiguration"),"name")
                            .AddSubscriberConfiguration(context.Configuration.GetSection("RabbitConfiguration2"), "name2")
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
