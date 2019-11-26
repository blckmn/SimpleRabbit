using SimpleRabbit.NetCore;
using SimpleRabbit.NetCore.Service;

namespace Subscriber.Dispatcher.Service.Models
{
    public class KeyedMessage : IDispatchModel
    {
        public string Key { get; set; }
        public string Message { get; set; }
        public BasicMessage BasicMessage { get; set; }
    }
}
