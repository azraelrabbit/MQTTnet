using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace MQTTnet.Internal
{
    public sealed class AsyncQueue<TItem> : IDisposable
    {
        readonly object _syncRoot = new object();
        //SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        Nito.AsyncEx.AsyncSemaphore _semaphore = new AsyncSemaphore(0);

        //Nito.AsyncEx.AsyncSemaphore se;
        //Nito.AsyncEx.AsyncManualResetEvent _semaphore = new AsyncManualResetEvent(false);
        ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();

        public int Count => _queue.Count;

        public void Enqueue(TItem item)
        {
            lock (_syncRoot)
            {
                
                //Nito.AsyncEx.AsyncSemaphoreSlim ap = new AsyncSemaphoreSlim(0);
           
                _queue.Enqueue(item);
                _semaphore?.Release();
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
 
                        task =   _semaphore.WaitAsync(cancellationToken);

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
                _semaphore = null;
            }
        }
    }
}
