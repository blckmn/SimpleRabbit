using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore.Model;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore.Service
{
    /// <summary>
    /// Asynchronous Dispatcher with ordering preservation where key is not inside the payload
    /// </summary>
    /// <remarks> Still relies on ordred queue dispatching, and processing of a message wrapper</remarks>
    public abstract class OrderedKeyDispatcherAsync : OrderedDispatcherAsync<MessageWrapper>
    {
        protected OrderedKeyDispatcherAsync(ILogger<OrderedKeyDispatcherAsync> logger) : base(logger)
        {
        }

        protected sealed override Task<MessageWrapper> Get(BasicMessage message)
        {
            return Task.FromResult(new MessageWrapper { });
        }

        protected abstract string GetKey(BasicMessage model);

        protected sealed override string GetKey(MessageWrapper model)
        {
            return GetKey(model.BasicMessage);
        }

        /// <summary>
        /// Process a message
        /// </summary>
        /// <param name="message"> the incoming rabbit message</param>
        /// <returns> return true if this is to acknowledge immediately</returns>
        /// <remarks>
        /// 
        /// Throw an exception if this message is to be unacknowledged
        /// 
        /// </remarks>
        protected abstract Task<bool> ProcessMessage(BasicMessage message);

        protected sealed override Task<bool> ProcessMessage(MessageWrapper message)
        {
            return ProcessMessage(message.BasicMessage);
        }
    }
}
