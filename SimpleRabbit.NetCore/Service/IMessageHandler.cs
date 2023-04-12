namespace SimpleRabbit.NetCore
{
    public interface IMessageHandler
    {
        /// <summary>
        /// Determine whether this handler will handle this message.
        /// </summary>
        /// <param name="tag"> the <see cref="QueueConfiguration.ConsumerTag"/> determined by which queue a message came from.</param>
        /// <returns>boolean true if this handler will take this message, false otherwise.</returns>
        bool CanProcess(string tag);

        /// <summary>
        /// Consume and process a message.
        /// </summary>
        /// <param name="message">A message containing headers and the raw data.</param>
        /// <returns><see cref="Acknowledgement"/> enum denoting how the message should be acknowledged.</returns>
        Acknowledgement Process(BasicMessage message);
    }
}
