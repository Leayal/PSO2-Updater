using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.ApplicationServices;

namespace PSO2_Updater_WPF
{
    class Program
    {
        [STAThread]
        static void Main(string[] arg)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyLoader.AssemblyResolve);

            Controller controller = new Controller();
            controller.Run(arg);
        }

        class Controller : WindowsFormsApplicationBase
        {
            App app;
            public Controller() : base(AuthenticationMode.Windows)
            {
                this.IsSingleInstance = true;
                this.SaveMySettingsOnExit = false;
                this.ShutdownStyle = ShutdownMode.AfterMainFormCloses;
                this.EnableVisualStyles = true;
                this.app = new App();
            }

            protected override bool OnStartup(StartupEventArgs eventArgs)
            {
                eventArgs.Cancel = true;
                this.app.Run();
                return false;
            }

            protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
            {
                if (this.app != null && this.app.MainWindow != null)
                    this.app.MainWindow.Activate();
            }
        }
    }
}
