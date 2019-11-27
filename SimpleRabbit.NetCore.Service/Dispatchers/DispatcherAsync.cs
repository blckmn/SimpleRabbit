using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Service
{
    public abstract class DispatcherAsync : IMessageHandlerAsync
    {
        private readonly ILogger<DispatcherAsync> _logger;

        protected DispatcherAsync(ILogger<DispatcherAsync> logger)
        {
            _logger = logger;
        }
        public abstract bool CanProcess(string tag);

        protected abstract Task<bool> ProcessMessage(BasicMessage msg);
        public Task<bool> Process(BasicMessage message)
        {
            // start a fire and forget task up
            _ = HandleMessage(message);

            // Acknowledgement will be handled by ProcessMessage
            return Task.FromResult(false);
        }

        private async Task HandleMessage(BasicMessage message)
        {
            try
            {
                if (await ProcessMessage(message))
                {
                    message.Ack();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while trying to handle a message in a dispatcher processing queue");
                message.ErrorAction?.Invoke();
            }
        }
    }
}
