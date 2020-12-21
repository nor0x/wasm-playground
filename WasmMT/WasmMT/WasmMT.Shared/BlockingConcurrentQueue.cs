using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace WasmMT.Shared
{
    public class BlockingConcurrentQueue<T> : ConcurrentQueue<T>
    {
        #region private methods

        private readonly AutoResetEvent _autoResetEvent;

        #endregion


        #region constructor

        public BlockingConcurrentQueue()
            : base()
        {
            this._autoResetEvent = new AutoResetEvent(false);
        }

        #endregion


        #region overrides

        /// <summary>
        /// Enqueues an item to queue and set signal for "TryPeekAwait".
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public void EnqueueSignalAwaiter(T item)
        {

            Debug.WriteLine("try EnqueueSignalAwaiter");
            try
            {

                if (item == null)
                {
                    throw new ArgumentNullException("item");
                }

                // enqueue
                this.Enqueue(item);

                // singal
                this._autoResetEvent.Set();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception EnqueueSignalAwaiter");
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Tries the peek item. Method blocks current task if queue is empty or until new item is enqueued over "EnqueueSignalAwaiter".
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public bool TryPeekAwait(out T result)
        {
            Debug.WriteLine("try TryPeekAwait");
            try
            {
                if (this.Count == 0)
                {
                    // wait
                    this._autoResetEvent.WaitOne();
                }

                // enqueue
                return this.TryPeek(out result);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception TryPeekAwait");
                Debug.WriteLine(e);
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// Tries the dequeue item. Method blocks current task if queue is empty or until new item is enqueued over "EnqueueSignalAwaiter".
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public bool TryDequeueAwait(out T result)
        {
            Debug.WriteLine("TryDequeueAwait EnqueueSignalAwaiter");
            try
            {
                if (this.Count == 0)
                {
                    // wait
                    this._autoResetEvent.WaitOne();
                }

                // enqueue
                return this.TryDequeue(out result);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception TryDequeueAwait");
                Debug.WriteLine(e);
                result = default(T);
                return true;

            }
        }

        public new void Enqueue(T item)
        {
            Debug.WriteLine("Enqueue EnqueueSignalAwaiter");
            try
            {
                base.Enqueue(item);
                this.RaiseChanged();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception Enqueue");
                Debug.WriteLine(e);
            }
        }

        public new bool TryDequeue(out T result)
        {
            Debug.WriteLine("try TryDequeue");
            try
            {
                var operationResult = base.TryDequeue(out result);
                this.RaiseChanged();
                return operationResult;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception TryDequeue");
                Debug.WriteLine(e);
                result = default(T);
                return false;
            }
        }

        public void Dispose()
        {
            // cancel waiting for new item
            // dependend threads can continue to work
            try
            {
                this._autoResetEvent.Set();
                this._autoResetEvent.Dispose();
            }
            catch (ObjectDisposedException) { }

        }

        #endregion


        #region private methods

        #region events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        private void RaiseChanged()
        {
            Debug.WriteLine("try RaiseChanged");
            try
            {
                if (this.Changed != null)
                {
                    this.Changed.Invoke(this, new EventArgs());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception RaiseChanged");
                Debug.WriteLine(e);
            }
        }

        #endregion
    }
}
