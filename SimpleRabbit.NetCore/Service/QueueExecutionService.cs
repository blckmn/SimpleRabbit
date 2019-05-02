using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRabbit.NetCore
{
    internal class QueueExecutionService
    {
        private readonly object _semaphore;

        public QueueExecutionService(object semaphore)
        {
            _semaphore = semaphore;
        }

        private readonly ManualResetEvent _mre = new ManualResetEvent(false);

        private readonly List<QueuedMessage> _queue = new List<QueuedMessage>();
        private Task Task { get; set; }
        public void Enqueue(QueuedMessage message)
        {
            lock (_semaphore)
            {
                _queue.Add(message);
            }

            _mre.Set();
        }

        public bool CanEnqueue(string key)
        {
            switch (key)
            {
                case null:
                    lock (_semaphore)
                    {
                        return _queue.Count == 0;
                    }
                default:
                    lock (_semaphore)
                    {
                        return _queue.Any(m => m.Key?.Equals(key, StringComparison.CurrentCultureIgnoreCase) ?? false);
                    }
            }
        }

        private void Process()
        {
            while (!_stopped)
            {
                QueuedMessage queuedMessage;
                lock (_semaphore)
                {
                    queuedMessage = _queue.FirstOrDefault();
                }

                if (queuedMessage == null)
                {
                    _mre.Reset();
                    _mre.WaitOne();
                    continue;
                }

                try
                {
                    if (queuedMessage.Action?.Invoke(queuedMessage.Message) ?? false)
                    {
                        queuedMessage.Message.Channel?.BasicAck(queuedMessage.Message.DeliveryTag, false);
                    }

                    lock (_semaphore)
                    {
                        _queue.Remove(queuedMessage);
                    }
                }
                catch
                {
                    queuedMessage.Message.RegisterError?.Invoke();
                    lock (_semaphore)
                    {
                        _queue.Clear();
                    }
                }
            }
        }

        public void Pause()
        {
            _mre.Reset();
        }

        private bool _stopped = true;
        public void Start()
        {
            if (Task == null)
            {
                Task = Task.Run(()=>Process());
            }

            _stopped = false;
            _mre.Reset();
        }

        public void Stop()
        {
            _stopped = true;
            _mre.Set();
            Task?.Wait();
            Task = null;
        }
    }
}