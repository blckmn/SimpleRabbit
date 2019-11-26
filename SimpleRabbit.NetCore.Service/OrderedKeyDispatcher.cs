using Microsoft.Extensions.Logging;
using SimpleRabbit.NetCore.Model;

namespace SimpleRabbit.NetCore.Service
{
    /// <summary>
    /// Dispatcher with ordering preservation where key is not inside the payload
    /// </summary>
    /// <remarks> Still relies on ordred queue dispatching, and processing of a message wrapper</remarks>
    public abstract class OrderedKeyDispatcher : OrderedDispatcher<MessageWrapper>
    {
        protected OrderedKeyDispatcher(ILogger<OrderedKeyDispatcher> logger) : base(logger)
        {
        }

        protected sealed override MessageWrapper Get(BasicMessage message)
        {
            return new MessageWrapper { };
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
        protected abstract bool ProcessMessage(BasicMessage message);

        protected sealed override bool ProcessMessage(MessageWrapper message)
        {
            return ProcessMessage(message.BasicMessage);
        }
    }
}
