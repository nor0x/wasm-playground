using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace TestLib.Internal
{
    public static class TaskFactoryExtension
    {
#if WASM
        private static void StartThread(Action threadAction)
        {
            Debug.WriteLine("StartThread: " + threadAction.ToString());
            // uncomment this line to enable background task start
             new Thread(() => threadAction()).Start();
        }
#endif

        public static Task StartLongRunningTask(this TaskFactory factory, Func<CancellationToken, Task> methodToStartInThread, CancellationToken token)
        {
#if WASM
            Debug.WriteLine("StartLongRunningTask WASM");
            var tcs = new TaskCompletionSource<bool>();
            StartThread(() => { methodToStartInThread(token).Wait(token); tcs.SetResult(false); });
            return tcs.Task;
#else
            Debug.WriteLine("StartLongRunningTask STANDARD");
            return factory.StartNew(() =>
            {
                try
                {
                    methodToStartInThread(token).Wait(token);
                }
                catch (OperationCanceledException) { }

            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
#endif
        }

        public static Task StartLongRunningTask(this TaskFactory factory, Action<CancellationToken> methodToStartInThread, CancellationToken token)
        {
            Debug.WriteLine("StartLongRunningTask: " + token.ToString());
#if WASM
            Debug.WriteLine("StartLongRunningTask WASM");
            var tcs = new TaskCompletionSource<bool>();
            StartThread(() => { methodToStartInThread(token); tcs.SetResult(false); });
            return tcs.Task;
#else
            Debug.WriteLine("StartLongRunningTask STANDARD");

            return factory.StartNew(() =>
            {
                try
                {
                    methodToStartInThread(token);
                }
                catch (OperationCanceledException) { }

            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
#endif
        }
    }
}
