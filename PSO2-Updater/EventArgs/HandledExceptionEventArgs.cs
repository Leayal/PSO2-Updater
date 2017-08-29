using System;

namespace Leayal.PSO2.Updater.Events
{
    public class HandledExceptionEventArgs : EventArgs
    {
        public Exception Error { get; private set; }
        public HandledExceptionEventArgs(Exception ex) : base()
        {
            this.Error = ex;
        }

        public override string ToString()
        {
            return this.Error.ToString();
        }
    }
}
