using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleRabbit.NetCore.Publisher
{
    /// <summary>
    /// A Factory that will instantiate singleton publisher instance for each configuration
    /// </summary>
    /// <remarks>
    /// Relies On <see cref="RabbitConfiguration"/> to be configured before this method to provide it configurations.
    /// </remarks>
    public class PublisherFactory : IDisposable
    {
        private readonly IOptionsMonitor<RabbitConfiguration> _optionsMonitor;
        private readonly IServiceProvider _provider;
        public ConcurrentDictionary<string, IPublishService> _publishers;
        public PublisherFactory(IOptionsMonitor<RabbitConfiguration> optionsMonitor, IServiceProvider provider)
        {
            _publishers = new ConcurrentDictionary<string, IPublishService>();
            _optionsMonitor = optionsMonitor;
            _provider = provider;
            _optionsMonitor.OnChange((config, name) =>
            {
                // a new one will be created when requested
                if (_publishers.TryRemove(name, out var service))
                {
                    service.Dispose();
                }
            });
        }

        public IPublishService GetPublisher(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var publisher = _publishers.GetOrAdd(name, CreatePublisher);

            return publisher;
        }

        private IPublishService CreatePublisher(string name)
        {
            var options = _optionsMonitor.Get(name);
            var publisher = new PublishService(_provider.GetService<ILogger<PublishService>>(), options);

            return publisher;
        }

        public void Dispose()
        {
            // Clean up any remaining services.
            foreach (var publisher in _publishers.Values)
            {
                publisher.Dispose();
            }
        }
    }
}
