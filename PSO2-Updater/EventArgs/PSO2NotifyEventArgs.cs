using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Leayal.PSO2.Updater.Events
{
    public class PSO2NotifyEventArgs : EventArgs
    {
        public string NewClientVersion { get; }
        public IReadOnlyDictionary<PSO2File, Exception> FailedList { get; }
        public bool Cancelled { get; }
        public string InstalledLocation { get; }
        internal PSO2NotifyEventArgs(string _ver, bool install, IReadOnlyDictionary<PSO2File, Exception> _failedlist) : base()
        {
            this.NewClientVersion = _ver;
            this.InstalledLocation = string.Empty;
            this.Cancelled = false;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(string _ver, string _installedlocation) : this(_ver, _installedlocation, null) { }

        internal PSO2NotifyEventArgs(string _ver, string _installedlocation, IReadOnlyDictionary<PSO2File, Exception> _failedlist) : base()
        {
            this.NewClientVersion = _ver;
            this.InstalledLocation = _installedlocation;
            this.Cancelled = false;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(bool _cancel, string _installedlocation, IReadOnlyDictionary<PSO2File, Exception> _failedlist) : base()
        {
            this.NewClientVersion = string.Empty;
            this.InstalledLocation = _installedlocation;
            this.Cancelled = _cancel;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(bool _cancel, IReadOnlyDictionary<PSO2File, Exception> _failedlist) : base()
        {
            this.NewClientVersion = string.Empty;
            this.InstalledLocation = string.Empty;
            this.Cancelled = _cancel;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(string _ver) : this(_ver, null) { }
    }
}
