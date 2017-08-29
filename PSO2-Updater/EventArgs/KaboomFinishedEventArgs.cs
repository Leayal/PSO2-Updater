using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Leayal.PSO2.Updater.Events
{
    internal class KaboomFinishedEventArgs : EventArgs
    {
        public object UserToken { get; }
        public Exception Error { get; }
        public UpdateResult Result { get; }
        public ReadOnlyCollection<string> FailedList { get; }

        internal KaboomFinishedEventArgs(UpdateResult _result, IEnumerable<string> _failedList, Exception ex, object _usertoken) : base()
        {
            this.Result = _result;
            if (_failedList != null)
                this.FailedList = new ReadOnlyCollection<string>(_failedList.ToArray());
            this.Error = ex;
            this.UserToken = _usertoken;
        }

        internal KaboomFinishedEventArgs(UpdateResult _result, IEnumerable<string> _failedList) : this(_result, _failedList, null, null) { }
        internal KaboomFinishedEventArgs(UpdateResult _result, Exception ex) : this(_result, null, ex, null) { }
        internal KaboomFinishedEventArgs(Exception ex) : this(UpdateResult.Failed, null, ex, null) { }
        internal KaboomFinishedEventArgs(UpdateResult _result) : this(_result, null, null, null) { }
    }
}
