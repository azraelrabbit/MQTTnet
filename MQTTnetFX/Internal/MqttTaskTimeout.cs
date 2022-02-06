using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;

namespace MQTTnet.Internal
{
    public static class MqttTaskTimeout
    {
        public static async Task WaitAsync(Func<CancellationToken, Task> action, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            //var timeoutCts = new CancellationTokenSource(timeout)
            //using (var timeoutCts = new CancellationTokenSource())
            using (var timeoutCts = new CancellationTokenSource())
            {
               
                //timeoutCts.CancelAfter(timeout);
                
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                {
                    try
                    {
                        await action(linkedCts.Token).ConfigureAwait(false);
                        //todo: on windowxp and .net fx 4.0 issue
                        //await action(linkedCts.Token);
                        //await action(linkedCts.Token);
                    }
                    catch (OperationCanceledException exception)
                    {
                        //Console.WriteLine(exception.Message + exception.StackTrace);
                        var timeoutReached = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
                        if (timeoutReached)
                        {

                            throw new MqttCommunicationTimedOutException(exception);
                        }
                        //Console.WriteLine(exception.Message + exception.StackTrace);
                        throw;
                    }
                }
            }
            
        }

        public static async Task<TResult> WaitAsync<TResult>(Func<CancellationToken, Task<TResult>> action, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            using (var timeoutCts = new CancellationTokenSource())
            {
                //timeoutCts.CancelAfter(timeout);
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                {
                    try
                    {
                        return await action(linkedCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException exception)
                    {
                        var timeoutReached = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
                        if (timeoutReached)
                        {
                            throw new MqttCommunicationTimedOutException(exception);
                        }

                        throw;
                    }
                }
            }
        }     
    }
}
