using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PSO2_Updater_Console.Helpers
{
    class DummyForm : Form
    {
        protected override bool ShowWithoutActivation => true;
        public DummyForm() : base()
        {
            this.Opacity = 0;
            this.ShowInTaskbar = false;
            this.ShowIcon = false;
            this.Visible = false;
        }

        protected override void OnShown(EventArgs e)
        {
            this.Visible = false;
            base.OnShown(e);
        }

        protected override void OnPaint(PaintEventArgs e) { }
        protected override void OnPaintBackground(PaintEventArgs e) { }
    }
}
