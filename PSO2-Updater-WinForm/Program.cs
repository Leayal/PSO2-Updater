using System;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace PSO2_Updater_WinForm
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ResolveEventHandler ev = new ResolveEventHandler(Helpers.AssemblyLoader.AssemblyResolve);
            AppDomain.CurrentDomain.AssemblyResolve += ev;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            WinFormController controller = new WinFormController();
            controller.Run(System.Environment.GetCommandLineArgs());

            AppDomain.CurrentDomain.AssemblyResolve -= ev;
        }

        private class WinFormController : WindowsFormsApplicationBase
        {
            internal WinFormController() : base(AuthenticationMode.Windows)
            {
                this.IsSingleInstance = true;
                this.EnableVisualStyles = true;
                this.SaveMySettingsOnExit = false;
            }

            protected override void OnCreateMainForm()
            {
                this.MainForm = new Forms.MyMainMenu();
            }
        }
    }
}
