using System;
using System.Linq;
using System.Windows;

namespace SalesManagement_SysDev
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Contains("--test-client") || e.Args.Contains("--test"))
            {
                int exitCode = ClientServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            base.OnStartup(e);
        }
    }
}
