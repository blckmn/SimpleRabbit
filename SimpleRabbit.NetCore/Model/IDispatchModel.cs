namespace SimpleRabbit.NetCore
{
    /// <summary>
    /// Model to implement in order to use an Ordered dispatcher
    /// 
    /// </summary>
    /// <remarks>
    /// The basic message will be used to perform channel operations.
    /// 
    /// </remarks>
    public interface IDispatchModel
    {
        BasicMessage BasicMessage { get; set; }
    }
}
