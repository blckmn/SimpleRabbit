using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Service
{
    public abstract class Dispatcher : IMessageHandler
    {
        private readonly ILogger<Dispatcher> _logger;

        protected Dispatcher(ILogger<Dispatcher> logger)
        {
            _logger = logger;
        }
        public abstract bool CanProcess(string tag);

        protected abstract bool ProcessMessage(BasicMessage msg);
        public bool Process(BasicMessage message)
        {
            // start a fire and forget task up
            Task.Run(() => HandleMessage(message));

            // Acknowledgement will be handled by ProcessMessage
            return false;
        }


        private void HandleMessage(BasicMessage message)
        {
            try
            {
                if (ProcessMessage(message))
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
