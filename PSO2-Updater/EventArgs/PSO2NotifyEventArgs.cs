using System;

namespace Leayal.PSO2.Updater.Events
{
    public class PSO2NotifyEventArgs : EventArgs
    {
        public string NewClientVersion { get; }
        public bool Installation { get; }
        public System.Collections.ObjectModel.ReadOnlyCollection<string> FailedList { get; }
        public bool Cancelled { get; }
        public string InstalledLocation { get; }
        internal PSO2NotifyEventArgs(string _ver, bool install, System.Collections.ObjectModel.ReadOnlyCollection<string> _failedlist) : base()
        {
            this.NewClientVersion = _ver;
            this.Installation = install;
            this.InstalledLocation = string.Empty;
            this.Cancelled = false;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(string _ver, string _installedlocation) : this(_ver, _installedlocation, null) { }

        internal PSO2NotifyEventArgs(string _ver, string _installedlocation, System.Collections.ObjectModel.ReadOnlyCollection<string> _failedlist) : base()
        {
            this.NewClientVersion = _ver;
            this.Installation = true;
            this.InstalledLocation = _installedlocation;
            this.Cancelled = false;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(bool _cancel, string _installedlocation, System.Collections.ObjectModel.ReadOnlyCollection<string> _failedlist) : base()
        {
            this.NewClientVersion = string.Empty;
            this.Installation = true;
            this.InstalledLocation = _installedlocation;
            this.Cancelled = _cancel;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(bool _cancel, bool install, System.Collections.ObjectModel.ReadOnlyCollection<string> _failedlist) : base()
        {
            this.NewClientVersion = string.Empty;
            this.Installation = install;
            this.InstalledLocation = string.Empty;
            this.Cancelled = _cancel;
            this.FailedList = _failedlist;
        }

        internal PSO2NotifyEventArgs(string _ver, bool install) : this(_ver, install, null) { }
    }
}
