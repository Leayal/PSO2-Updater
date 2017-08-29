using System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Collections.Generic;
using Leayal.Net;
using Leayal.PSO2.Updater.Events;

namespace Leayal.PSO2.Updater
{
    internal class BackgroundWorkerManager : IEnumerable<ExtendedBackgroundWorker>, IDisposable
    {

        private List<ExtendedBackgroundWorker> total;
        private ConcurrentBag<ExtendedBackgroundWorker> resting;

        public BackgroundWorkerManager(int _maxcount)
        {
            this._Count = 0;
            this.MaxCount = _maxcount;
            this.total = new List<ExtendedBackgroundWorker>();
            this.resting = new ConcurrentBag<ExtendedBackgroundWorker>();
        }

        internal BackgroundWorkerManager() : this(0) { }

        private int _Count;
        public int Count { get { return this._Count; } }

        private int _MaxCount;
        public int MaxCount
        {
            get { return this._MaxCount; }
            set
            {
                this._MaxCount = value;
                this.AdjustNumberOfBWorker();
            }
        }

        public void CancelAsync()
        {
            for (int i = 0; i < this.total.Count; i++)
                if (this.total[i].IsBusy)
                    this.total[i].CancelAsync();
        }

        public int GetNumberOfRunning()
        {
            return (this.total.Count - this.resting.Count);
        }

        private void AdjustNumberOfBWorker()
        {
            if (this.Count == this.MaxCount)
            { return; }
            else
            {
                if (this.Count < this.MaxCount)
                    this.Add(new ExtendedBackgroundWorker());
                else if (this.Count > this.MaxCount)
                    this.Remove();
                this.AdjustNumberOfBWorker();
            }
        }

        private void Add(ExtendedBackgroundWorker item)
        {
            item.WorkerSupportsCancellation = true;
            item.FinishedWorking += this.Item_FinishedWorking;
            this.WorkerAdded?.Invoke(this, new ExtendedBackgroundWorkerEventArgs(item));
            this.total.Add(item);
            this.resting.Add(item);
            Interlocked.Increment(ref this._Count);
        }

        public event EventHandler<ExtendedBackgroundWorkerEventArgs> WorkerAdded;

        private void Item_FinishedWorking(object sender, RunWorkerCompletedEventArgs e)
        {
            ExtendedBackgroundWorker bw = sender as ExtendedBackgroundWorker;
            if (bw != null)
                this.resting.Add(bw);
        }

        public ExtendedBackgroundWorker GetRestingWorker()
        {
            ExtendedBackgroundWorker bw = null;
            if (!this.resting.TryTake(out bw))
                return null;
            return bw;
        }

        public void Start()
        {
            ExtendedBackgroundWorker bw;
            while (this.resting.TryTake(out bw))
                if (!bw.IsBusy)
                    bw.RunWorkerAsync();
        }

        public void Clear()
        {
            this.MaxCount = 0;
        }

        public IEnumerator<ExtendedBackgroundWorker> GetEnumerator()
        {
            return this.resting.GetEnumerator();
        }

        private bool Remove()
        {
            ExtendedBackgroundWorker baaa;
            bool re = false;
            if (!this.resting.IsEmpty)
            {
                if (this.resting.TryTake(out baaa))
                    re = this.Remove(baaa);
            }
            if (this.total.Count > 0)
            {
                baaa = this.total.Find(bw => bw.IsBusy);
                if (baaa != null)
                    re = this.Remove(baaa);
            }
            return re;
        }

        private bool Remove(ExtendedBackgroundWorker item)
        {
            if (this.total.Count > 0)
            {
                if (this.total.Contains(item))
                {
                    this.total.Remove(item);
                    item.FinishedWorking += this.Item_FinishedWorking;
                    Interlocked.Decrement(ref this._Count);
                    this.WorkerRemoved?.Invoke(this, new ExtendedBackgroundWorkerEventArgs(item));
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public event EventHandler<ExtendedBackgroundWorkerEventArgs> WorkerRemoved;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.total.GetEnumerator();
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ExtendedBackgroundWorker asd;
            while (this.total.Count > 0)
            {
                asd = this.total[0];
                this.total.Remove(asd);
                if (asd.IsBusy)
                    asd.CancelAsync();
            }
            while (!this.resting.IsEmpty)
                this.resting.TryTake(out asd);
        }
    }

    internal class ExtendedBackgroundWorker : BackgroundWorker
    {
        public ExtendedWebClient WebClient { get; }
        internal ExtendedBackgroundWorker() : base()
        {
            this.WebClient = new ExtendedWebClient();
            this.WebClient.UserAgent = DefaultValues.Web.UserAgent;
        }

        public new void RunWorkerAsync()
        {
            this.StartWorking?.Invoke(this, System.EventArgs.Empty);
            base.RunWorkerAsync();
        }

        public new void CancelAsync()
        {
            this.WebClient.CancelAsync();
            base.CancelAsync();
        }

        public new void Dispose()
        {
            base.Dispose();
            this.WebClient.Dispose();
        }

        public new void RunWorkerAsync(object argument)
        {
            this.StartWorking?.Invoke(this, System.EventArgs.Empty);
            base.RunWorkerAsync(argument);
        }

        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            this.FinishedWorking?.Invoke(this, e);
            base.OnRunWorkerCompleted(e);
        }

        internal event RunWorkerCompletedEventHandler FinishedWorking;
        internal event EventHandler StartWorking;
    }
}
