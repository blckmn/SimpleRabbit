﻿namespace SimpleRabbit.NetCore
{
    /// <summary>
    /// Configurable options for acknowledging messages
    /// </summary>
    public enum Acknowledgement 
    {
        /// <summary>
        /// Acknowledge the message
        /// </summary>
        Ack = 1,

        /// <summary>
        /// Do not acknowledge the message; requeue it on original queue
        /// </summary>
        NackRequeue = 2,

        /// <summary>
        /// Do not acknowledge the message; send it to dead letter queue
        /// </summary>
        NackDeadLetter = 3
    }

    public interface IMessageHandler
    {
        /// <summary>
        /// Determine whether this handler will handle this message
        /// </summary>
        /// <param name="tag"> the <see cref="QueueConfiguration.ConsumerTag"/> determined by which queue a message came from</param>
        /// <returns>boolean true if this handler will take this message, false otherwise</returns>
        bool CanProcess(string tag);
        
        /// <summary>
        /// Consume a message 
        /// </summary>
        /// <param name="message">A message containing headers and the raw data</param>
        /// <returns>flag indicating if this message should be acknowledged</returns>
        /// <remarks>
        /// <para>
        /// return true if you want to ack immediately. 
        /// false allows for delegation of the acknowledgement, either by dispatching to a thread or acknowledging later.
        /// </para>
        /// </remarks>
        Acknowledgement Process(BasicMessage message);
    }


}
