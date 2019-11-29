using SimpleRabbit.NetCore;
using System.Threading.Tasks;

namespace Subscriber.Service.Service
{
    public delegate Task AsyncEventHandler<in TEvent>(object sender, TEvent @event);
    public class MessageProcessorAsync : IMessageHandlerAsync
    {
        public AsyncEventHandler<BasicMessage> Handler { get; set; }
        public bool CanProcess(string tag)
        {
            return true;
        }

        public async Task<bool> Process(BasicMessage message)
        {
            await Handler(this, message);


            return true;
        }
    }
}
