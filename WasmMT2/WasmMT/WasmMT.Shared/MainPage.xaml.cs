using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using TestLib;
using WasmMT.Shared;
using SkiaSharp.Views.UWP;
using SkiaSharp;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WasmMT.Shared.Factory;

#if WASM
using WasmMT.Wasm;

#elif WINDOWS_UWP
	using WasmMT.UWP;
#else
	using TestLib.Internal;
#endif

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WasmMT
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{

		public MainPage()
		{
			this.InitializeComponent();
			_queue = new BlockingConcurrentQueue<string>();

		}

		float x = 300;
		float y = 200;
		float radius = 50;
		float incr = 18.5f;


		Random rand = new Random();

		private void updateCircle()
        {
			if(rand.NextDouble()< 0.5)
            {
				x += incr;
				radius += rand.Next(1, 5);
            }
			else
            {
				x -= incr;
				radius -= rand.Next(1, 5);


			}
			if (rand.NextDouble() < 0.5)
			{
				y += incr;

			}
			else
			{
				y -= incr;
			}

		}

		private void LibCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			if (statusTextBlock != null)
			{
				statusTextBlock.Text = "run in shared";
			}
		}

		private void LibCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			if (statusTextBlock != null)
			{
				statusTextBlock.Text = "run in lib";
			}
		}

		private async void StartButton_Click(object sender, RoutedEventArgs e)
		{
			if (LibCheckBox.IsChecked is bool check)
			{
				if (check)
				{
					Debug.WriteLine("run in shared");

					await Run();
					await PrintQueue();
				}
				else
				{
					Debug.WriteLine("run in lib");
					var tester = new ThreadTester();
					await tester.Run();
					while (tester.TestQueue.IsEmpty == false)
					{
						var s = string.Empty;
						var b = tester.TestQueue.TryDequeue(out s);
						messageTextBlock.Text = (s); ;
						await Task.Delay(1000);
					}
				}
			}
		}

		BlockingConcurrentQueue<string> _queue;



		async Task PrintQueue()
		{
			while (_queue.IsEmpty == false)
			{
				var s = string.Empty;
				var b = _queue.TryDequeue(out s);
				messageTextBlock.Text = (s); ;
				await Task.Delay(1000);
			}
		}


		void LogMessage(string message)
		{
			Debug.WriteLine($"[tid:{Thread.CurrentThread.ManagedThreadId}] {message}");
			_queue.Enqueue($"[tid:{Thread.CurrentThread.ManagedThreadId}] {message}");
		}

		async Task Run()
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
		ThreadTester threadTester = null;
		private async void CancelTaskButton_Clicked(object sender, RoutedEventArgs e)
		{
			if (threadTester == null)
			{
				threadTester = new ThreadTester();
				threadTester.TextChanged += (s, args) =>
				{
                    Debug.WriteLine("text changed");
					TaskTextBlock.Text = threadTester.OutputText;
				};
			}
			if (LibCheckBox.IsChecked is bool check)
			{
				if (check)
				{
					Debug.WriteLine("in shared");

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
				else
				{
                    Debug.WriteLine("in lib");
					threadTester.StartTask();
				}
			}
		}

		public async Task EnqueueThings(CancellationToken cancel)
		{
            Debug.WriteLine("ENQUEUE cancel? " + cancel.IsCancellationRequested);
			while (cancel.IsCancellationRequested == false)
			{
				var thingy =DateTime.Now.ToString();
				await Task.Delay(250);
				Debug.WriteLine(thingy + " enqueued");
#if WINDOWS_UWP
				await Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
				{
					TaskTextBlock.Text = "enqueing: " + thingy + "             queue count: " + _queue.Count();
				});
#endif
#if WASM
				TaskTextBlock.Text = "enqueing: " + thingy + "             queue count: " + _queue.Count();
#endif
				_queue.EnqueueSignalAwaiter(thingy);
			}
		}

		public async Task DequeueThings(CancellationToken cancel)
		{
			Debug.WriteLine("DEQUEUE cancel? " + cancel.IsCancellationRequested);

			while (cancel.IsCancellationRequested == false)
			{

				string thingy = "";
				if (_queue.TryPeekAwait(out thingy))
				{
					await Task.Delay(50);
#if WINDOWS_UWP

					await Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
					{ 
						TaskTextBlock.Text = "dequeued: " + thingy + "             queue count: " + _queue.Count();
					});
#endif
#if WASM
					TaskTextBlock.Text = "dequeued: " + thingy + "             queue count: " + _queue.Count();
#endif
					_queue.TryDequeue(out thingy);
					Debug.WriteLine("dequeued: " + thingy);
				}
				if (_queue.Count() == 0)
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
