namespace SimpleRabbit.NetCore.Model
{
    public class MessageWrapper<T> : IDispatchModel where T : class
    {
        public T Model { get; set; }
        public BasicMessage BasicMessage { get; set; }
    }
    public class MessageWrapper : IDispatchModel
    {
        public BasicMessage BasicMessage { get; set; }
    }
}
