﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                        var config = context.Configuration;
                        services
                            .AddSingletonMessageHandler<MessageProcessor>()
                            .AddRabbitConfiguration("name",config.GetSection("RabbitConfiguration"))
                            .AddRabbitConfiguration("name2",config.GetSection("RabbitConfiguration2"))
                            .AddSubscriberConfiguration("name",context.Configuration.GetSection("RabbitConfiguration:Subscribers"))
                            .AddSubscriberConfiguration("name2",context.Configuration.GetSection("RabbitConfiguration2:Subscribers"))
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
