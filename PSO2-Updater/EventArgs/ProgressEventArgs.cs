using System;

namespace Leayal.PSO2.Updater.Events
{
    public class ProgressEventArgs : EventArgs
    {
        public int Progress { get; }
        public ProgressEventArgs(int _progress) : base()
        {
            this.Progress = _progress;
        }
    }
}
