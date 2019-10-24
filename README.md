# SimpleRabbit

An easy wrapper for the RabbitMQ client that allows inclusion in DotNetCore projects. Can be used with the standard dependency injection provided in DotNetCore.

## Getting started

### Package Install

There are two packages on Nuget.
1. SimpleRabbit.Netcore <- has the basics to be able to publish - and listen to queues where not hosting.
2. SimpleRabbit.Netcore.Service <- has the IHostedService option for running as a host to listen to queues.

Installing is as easy as: `dotnet add package SimpleRabbit.NetCore` or `Install-Package SimpleRabbit.NetCore` depending on your setup.

### Publishing

```
    private static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .Build();

        var services = new ServiceCollection();
        services
            .AddPublisherServices()
            .AddRabbitConfiguration(configuration);

        var provider = services.BuildServiceProvider();

        var publisher = provider.GetService<IPublishService>();
        publisher.Publish(exchange: "ExchangeName", body: "Test body");
    }
```

The appsettings.json file (to provide connectivity to rabbit):
```
    {
        "RabbitConfiguration": {
            "Uri": "amqp://username:password@hostname/"
        }
    }
```

### Subscribing

```
    public static async Task Main(string[] args)
    {
        var builder = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", true);
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions();

                /* the rabbit services */
                services
                    .AddSubscriberServices()
                    .AddRabbitConfiguration(context.Configuration)
                    .AddSubscriberConfiguration(context.Configuration)
                    .AddSingleton<IMessageHandler, Processor>();
            });

        await builder.RunConsoleAsync();
    }
```

The message handler is chosen based on the CanProcess call. The consumer tag is passed in i.e. tags are matched not queues. This allows a handler to handle multiple messages from multiple queues.
```
    internal class Processor : IMessageHandler
    {
        public bool CanProcess(string tag)
        {
            /* validate whether this handler will handle this tag */
            return true;
        }

        public bool Process(BasicMessage message)
        {
            var body = message.Body;

            if (string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine($"Message contents: {body}");
            }
            else
            {
                Console.WriteLine($"Empty message: {message.MessageId}")
            }
            /* returning false, and the message will be Nack'd and requeued */
            return true;
        }
    }
```

Subscribers are a list (of queues to consume), and they are auto wired up to the queue and are eventing based. The message handler for a given queue is chosen based on matching consumer tags.
```
    {
        "RabbitConfiguration": {
            "Uri": "amqp://username:password@hostname/"
        },
        "Subscribers": [
            {
                "ConsumerTag": "TestTagName",
                "QueueName": "Test"
            }
        ]
    }
```

### Extra configuration

Additional hostnames can be provided for round robin if required:
```
    {
        "RabbitConfiguration": {
            "Uri": "amqp://username:password@hostname/",
            "Hostnames": [
                "host1",
                "host2"
            ]
        }
    }
```

### Making use of multiple Rabbit clusters

Rather than binding using the default extensions, you can configure it yourself and make use of multiple configurations.
```
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .Build();

            PublishService PublisherFactory(IServiceProvider service, string name)
            {
                var p = service.GetService<PublishService>();
                p.ConfigurationName = name;
                return p;
            }

            var services = new ServiceCollection();
            services
                .Configure<RabbitConfiguration>("Configuration1", configuration.GetSection("RabbitConfiguration:Configuration1"))
                .Configure<RabbitConfiguration>("Configuration2", configuration.GetSection("RabbitConfiguration:Configuration2"))
                .AddTransient<PublishService>()
                .AddTransient<IPublishService>(c => PublisherFactory(c, "Configuration1"))
                .AddTransient<IPublishService>(c => PublisherFactory(c, "Configuration2"))
                .AddTransient(c => c.GetServices<IPublishService>().ToList());

            var provider = services.BuildServiceProvider();

            var publisher = provider.GetService<List<IPublishService>>().FirstOrDefault(p => p.ConfigurationName.Equals("Configuration1"));
            publisher?.Publish(exchange: "ExchangeName", body: "Test body");
        }
    }
```
This will give you the ability to inject a `List<IPublishService>` into your class and you can then select the publisher based on the configuration name. 

The `appsettings.json` file will look something like this:
```
{
  "RabbitConfiguration": {
    "Configuration1": {
      "Uri": "amqp://username:password@hostname/"
    },
    "Configuration2": {
      "Uri": "amqp://username:password@hostname/"
    }
  }
}
```

Note the multiple configuration setup is explained here: https://andrewlock.net/using-multiple-instances-of-strongly-typed-settings-with-named-options-in-net-core-2-x/
