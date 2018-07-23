# SimpleRabbit

An easy wrapper for the RabbitMQ client that allows inclusion in DotNetCore projects. Can be used with the standard dependency injection provided in DotNetCore.

## Getting started

### Publishing

```
    private static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<PublisherProcess>();
        services.AddPublisherServices(configuration);

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
                services.AddSubscriberServices(context.Configuration);
                services.AddSingleton<IMessageHandler, Processor>();
            });

        await builder.RunConsoleAsync();
    }
```

The message handler is chosen based on the CanProcess call. The consumer tag is passed in i.e. tags are matched not queues.
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
                Console.WriteLine($"Empty message: {message.BasicProperties?.MessageId}")
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