namespace SimpleRabbit.NetCore
{
    public interface IMessageHandler
    {
        bool CanProcess(string tag);
        bool Process(BasicMessage message);
    }
}
