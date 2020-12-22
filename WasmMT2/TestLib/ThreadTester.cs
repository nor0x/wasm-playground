using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestLib.Internal;

namespace TestLib
{
    public class ThreadTester
    {
		static void Main()
        {

        }
		string outputText;
		public LibBlockingConcurrentQueue<string> TestQueue { get; set; }
		public event EventHandler TextChanged;
		public string OutputText
        {
			set
            {
				outputText = value;
				TextChanged?.Invoke(this, null);
            }
			get
            {
				return outputText;
            }
        }
		public ThreadTester()
        {
			 TestQueue = new LibBlockingConcurrentQueue<string>();
		}

		void LogMessage(string message)
		{
			Debug.WriteLine($"[tid:{Thread.CurrentThread.ManagedThreadId}] {message}");
			TestQueue.Enqueue($"[tid:{Thread.CurrentThread.ManagedThreadId}] {message}");
		}

		public async Task Run()
		{

			LogMessage("Startup");
			var evt = new ManualResetEvent(false);
			var tcs = new TaskCompletionSource<bool>();

			var t = new Thread(() =>
			{

				LogMessage($"Thread begin");

				tcs.SetResult(true);

				LogMessage($"Waiting for event");

				evt.WaitOne();

				LogMessage($"Got event, terminating thread");
			});

			t.Start();

			LogMessage($"Waiting for completion source");

			await tcs.Task;

			LogMessage($"Got task result, raising event");

			evt.Set();

			LogMessage($"Main thread exiting");
		}

		CancellationToken token1;
		CancellationToken token2;
		CancellationTokenSource source1;
		CancellationTokenSource source2;
		bool runTask = true;
		public void StartTask()
		{
			if (runTask)
			{
				Debug.WriteLine("StartLongRunningTask");
				runTask = false;
				source1 = new CancellationTokenSource();
				token1 = source1.Token;
				StartLongRunningTask(EnqueueThings, token1);
			}
			else
			{
				Debug.WriteLine("Cancel StartLongRunningTask");
				source2 = new CancellationTokenSource();
				token2 = source2.Token;
				source1.Cancel();
				StartLongRunningTask(DequeueThings, token2);

				runTask = true;

			}
		}

		public async Task EnqueueThings(CancellationToken cancel)
		{
			Debug.WriteLine("ENQUEUE cancel? " + cancel.IsCancellationRequested);
			while (cancel.IsCancellationRequested == false)
			{
				var thingy = DateTime.Now.ToString();
				await Task.Delay(250);
				Debug.WriteLine(thingy + " enqueued");
				OutputText = "enqueing: " + thingy + "             queue count: " + TestQueue.Count();
				TestQueue.EnqueueSignalAwaiter(thingy);
			}
		}

		public async Task DequeueThings(CancellationToken cancel)
		{
			Debug.WriteLine("DEQUEUE cancel? " + cancel.IsCancellationRequested);

			while (cancel.IsCancellationRequested == false)
			{

				string thingy = "";
				if (TestQueue.TryPeekAwait(out thingy))
				{
					await Task.Delay(50);
					TestQueue.TryDequeue(out thingy);
					OutputText = "dequeued: " + thingy + "             queue count: " + TestQueue.Count();
					Debug.WriteLine("dequeued: " + thingy);
				}
				if (TestQueue.Count() == 0)
				{
					Debug.WriteLine("cancel");
					source2.Cancel();
				}

			}
		}

		private Task StartLongRunningTask(Func<CancellationToken, Task> methodToStartInThread, CancellationToken token)
		{
			return Task.Factory.StartLongRunningTask(methodToStartInThread, token);
		}
	}
}