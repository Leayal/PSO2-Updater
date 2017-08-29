using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSO2_Updater_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            ResolveEventHandler ev = new ResolveEventHandler(Helpers.AssemblyLoader.AssemblyResolve);
            AppDomain.CurrentDomain.AssemblyResolve += ev;

            ConsoleController controller = new ConsoleController();
            controller.Run(args);
            AppDomain.CurrentDomain.AssemblyResolve -= ev;
        }
    }
}
