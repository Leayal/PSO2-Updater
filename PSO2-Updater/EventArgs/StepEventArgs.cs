using System;

namespace Leayal.PSO2.Updater.Events
{
    public class StepEventArgs : EventArgs
    {
        public UpdateStep Step { get; }
        public object Value { get; }

        public StepEventArgs(UpdateStep _step, object val) : base()
        {
            this.Step = _step;
            this.Value = val;
        }

        public StepEventArgs(UpdateStep _step) : this(_step, null) { }
    }
}
