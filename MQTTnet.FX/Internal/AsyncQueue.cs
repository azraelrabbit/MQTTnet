using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncQueue<TItem> : IDisposable
    {
        readonly object _syncRoot = new object();
        //AsyncLock _semaphore = new AsyncLock(0);

        Nito.AsyncEx.AsyncManualResetEvent _semaphore = new AsyncManualResetEvent(false);

        ConcurrentQueue <TItem> _queue = new ConcurrentQueue<TItem>();

        public int Count => _queue.Count;

        public void Enqueue(TItem item)
        {
            lock (_syncRoot)
            {
                _queue.Enqueue(item);
                //_semaphore?.Release();
                _semaphore.Set();


            }
        }

        public async Task<AsyncQueueDequeueResult<TItem>> TryDequeueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Task task;
                    lock (_syncRoot)
                    {
                        if (_semaphore == null)
                        {
                            return new AsyncQueueDequeueResult<TItem>(false, default);
                        }

                        //task = _semaphore.WaitAsync(cancellationToken);

                        task = _semaphore.WaitAsync();
                    }
                    
                    await task.ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new AsyncQueueDequeueResult<TItem>(false, default);
                    }

                    if (_queue.TryDequeue(out var item))
                    {
                        return new AsyncQueueDequeueResult<TItem>(true, item);
                    }
                }
                catch (ArgumentNullException)
                {
                    // The semaphore throws this internally sometimes.
                    return new AsyncQueueDequeueResult<TItem>(false, default);
                }
                catch (OperationCanceledException)
                {
                    return new AsyncQueueDequeueResult<TItem>(false, default);
                }
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
        }

        public AsyncQueueDequeueResult<TItem> TryDequeue()
        {
            if (_queue.TryDequeue(out var item))
            {
                return new AsyncQueueDequeueResult<TItem>(true, item);
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
        }

        public void Clear()
        {
            Interlocked.Exchange(ref _queue, new ConcurrentQueue<TItem>());
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                //_semaphore?.Dispose();

                _semaphore.Set();
                _semaphore = null;
            }
        }
    }
}
