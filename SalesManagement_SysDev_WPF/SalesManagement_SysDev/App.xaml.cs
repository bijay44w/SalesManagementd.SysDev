using System;
using System.Linq;
using System.Windows;

namespace SalesManagement_SysDev
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Contains("--test-modules"))
            {
                int exitCode = ModuleSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test-client"))
            {
                int exitCode = ClientServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test"))
            {
                int exit1 = ClientServiceSelfCheck.RunAllTests();
                int exit2 = ModuleSelfCheck.RunAllTests();
                Environment.Exit(exit1 != 0 ? exit1 : exit2);
                return;
            }

            base.OnStartup(e);
        }
    }
}
