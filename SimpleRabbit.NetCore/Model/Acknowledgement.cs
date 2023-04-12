namespace SimpleRabbit.NetCore
{
    /// <summary>
    /// Configurable options for acknowledging messages
    /// </summary>
    public enum Acknowledgement 
    {
        /// <summary>
        /// Acknowledge the message.
        /// </summary>
        Ack = 1,

        /// <summary>
        /// Do not acknowledge the message; requeue it on original queue.
        /// </summary>
        NackRequeue = 2,

        /// <summary>
        /// Do not acknowledge the message; send it to dead letter queue specified in x-dead-letter-exchange and x-dead-letter-routing-key arguments.
        /// </summary>
        NackDeadLetter = 3,

        /// <summary>
        /// Hack to enable async message handler to work correctly and handle acknowledgements separately from QueueService.
        /// </summary>
        Ignore = 4
    }
}