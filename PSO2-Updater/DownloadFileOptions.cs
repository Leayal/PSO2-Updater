using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2.Updater
{
    public class DownloadFileOptions
    {
        public event Action<Exception> HandledException;
        public event Action Cancelled;

        internal bool IsCancelledActionNull => (this.Cancelled == null);
        internal bool IsHandledExceptionActionNull => (this.HandledException == null);

        internal Action GetCancelled()
        {
            return this.Cancelled;
        }

        internal Action<Exception> GetHandledException()
        {
            return this.HandledException;
        }
    }
}
