using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WasmMT.Wasm.None
{
    public static class TaskFactoryExtension
    {

        private static void StartThread(Action threadAction)
        {
            Debug.WriteLine("StartThread: " + threadAction.ToString());
            // uncomment this line to enable background task start
             new Thread(() => threadAction()).Start();
        }

        public static Task StartLongRunningTask(this TaskFactory factory, Func<CancellationToken, Task> methodToStartInThread, CancellationToken token)
        {
            /*
            var tcs = new TaskCompletionSource<bool>();
            StartThread(() => { methodToStartInThread(token).Wait(token); tcs.SetResult(false); });
            return tcs.Task;
            */
            return factory.StartNew(() =>
            {
                try
                {
                    methodToStartInThread(token).Wait(token);
                }
                catch (OperationCanceledException) { }

            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
        }

        public static Task StartLongRunningTask(this TaskFactory factory, Action<CancellationToken> methodToStartInThread, CancellationToken token)
        {
            /*
            var tcs = new TaskCompletionSource<bool>();
            StartThread(() => { methodToStartInThread(token); tcs.SetResult(false); });
            return tcs.Task;
            */
            return factory.StartNew(() =>
            {
                try
                {
                    methodToStartInThread(token);
                }
                catch (OperationCanceledException) { }

            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
        }
    }
}
