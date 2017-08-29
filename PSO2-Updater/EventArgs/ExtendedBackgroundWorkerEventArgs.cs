using System;

namespace Leayal.PSO2.Updater.Events
{
    internal class ExtendedBackgroundWorkerEventArgs : EventArgs
    {
        public ExtendedBackgroundWorker Worker { get; }
        public ExtendedBackgroundWorkerEventArgs(ExtendedBackgroundWorker _worker) : base()
        {
            this.Worker = _worker;
        }
    }
}
