using System;

namespace Leayal.PSO2.Updater.Events
{
    public class ValidPrepatchPromptEventArgs : EventArgs
    {
        public bool Use { get; set; }
        internal ValidPrepatchPromptEventArgs(bool _use) : base()
        {
            this.Use = _use;
        }

        internal ValidPrepatchPromptEventArgs() : this(true) { }
    }

    public class InvalidPrepatchPromptEventArgs : EventArgs
    {
        public bool Delete { get; set; }
        internal InvalidPrepatchPromptEventArgs(bool _delete) : base()
        {
            this.Delete = _delete;
        }

        internal InvalidPrepatchPromptEventArgs() : this(false) { }
    }
}
