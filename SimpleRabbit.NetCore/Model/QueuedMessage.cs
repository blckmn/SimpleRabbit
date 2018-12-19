using System;

namespace SimpleRabbit.NetCore
{
    internal class QueuedMessage
    {
        public string Key { get; set; }
        public BasicMessage Message { get; set; }
        public Func<BasicMessage, bool> Action { get; set; }
    }
}