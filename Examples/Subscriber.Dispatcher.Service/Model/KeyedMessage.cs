using SimpleRabbit.NetCore;

namespace Subscriber.Dispatcher.Service.Models
{
    public class KeyedMessage
    {
        public string Key { get; set; }
        public string Message { get; set; }
        
    }

    public class Wrapper : IDispatchModel
    {
        public KeyedMessage Model { get; set; }
        public BasicMessage BasicMessage { get; set; }
    }
}
