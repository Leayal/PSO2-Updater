using System;

namespace Leayal.PSO2.Updater.Events
{
    internal class DetailedProgressChangedEventArgs : EventArgs
    {
        public int Current { get; }
        public int Total { get; }
        internal DetailedProgressChangedEventArgs(int _current, int _total) : base()
        {
            this.Total = _total;
            this.Current = _current;
        }

        internal DetailedProgressChangedEventArgs(int _current) : this(_current, 0) { }
    }
}
