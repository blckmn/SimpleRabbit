using System;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleRabbit.NetCore.Service
{

    /// <summary>
    /// Sempahore based asychronous locking, able to be used within a using block
    /// 
    /// </summary>
    /// <remarks>
    /// 
    /// Credit to Stephen Toub : https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-6-asynclock/
    /// 
    /// </remarks>
    /// <example>
    /// <code>
    /// AsyncLock _sempahore = new AsyncLock();
    /// 
    /// using ( await _sempahore.LockAsync())
    /// {
    ///     //critical section
    /// }
    /// </code>
    /// </example>
    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> m_releaser;

        public AsyncLock()
        {
            m_releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public Task<IDisposable> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                        m_releaser :
                        wait.ContinueWith((_, state) => (IDisposable)state,
                            m_releaser.Result, CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;
            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }
            public void Dispose() { m_toRelease.m_semaphore.Release(); }
        }
    }
}
