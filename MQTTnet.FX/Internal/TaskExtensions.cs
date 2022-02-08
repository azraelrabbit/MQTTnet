using MQTTnet.Diagnostics;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.Logger;
using System;
using Nito.AsyncEx;

namespace MQTTnet.Internal
{
    public static class TaskExtensions
    {
        public static void RunInBackground(this Task task, MqttNetSourceLogger logger = null)
        {
            task?.ContinueWith(t =>
                {
                    // Consume the exception first so that we get no exception regarding the not observed exception.
                    var exception = t.Exception;
                    logger?.Error(exception, "Unhandled exception in background task.");
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }


        public static async Task<bool> WaitAsync(this AsyncManualResetEvent waitHandle, int timeoutMs)
        {
            if (waitHandle == null)
                throw new ArgumentNullException("waitHandle");

            Task waitTask = waitHandle.WaitAsync();
            Task completedTask = await TaskEx.WhenAny(TaskEx.Delay(timeoutMs), waitHandle.WaitAsync());

            return completedTask == waitTask;
        }
    }
}
