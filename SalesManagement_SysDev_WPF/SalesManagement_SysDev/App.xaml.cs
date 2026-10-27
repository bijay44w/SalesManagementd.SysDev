using System;
using System.Linq;
using System.Windows;

namespace SalesManagement_SysDev
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 日本語コードページ(CP932 / Shift-JIS / CP1252)の文字化け復元サポート
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

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

            if (e.Args.Contains("--test-product"))
            {
                int exitCode = ProductServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test-stock"))
            {
                int exitCode = StockServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test-employee"))
            {
                int exitCode = EmployeeServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test-sales"))
            {
                int exitCode = SalesServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test-order"))
            {
                int exitCode = OrderServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test-chumon"))
            {
                int exitCode = ChumonServiceSelfCheck.RunAllTests();
                Environment.Exit(exitCode);
                return;
            }

            if (e.Args.Contains("--test"))
            {
                int exit1 = ClientServiceSelfCheck.RunAllTests();
                int exit2 = ProductServiceSelfCheck.RunAllTests();
                int exit3 = StockServiceSelfCheck.RunAllTests();
                int exit4 = EmployeeServiceSelfCheck.RunAllTests();
                int exit5 = SalesServiceSelfCheck.RunAllTests();
                int exit6 = OrderServiceSelfCheck.RunAllTests();
                int exit7 = ChumonServiceSelfCheck.RunAllTests();
                int exit8 = ModuleSelfCheck.RunAllTests();
                int finalExit = exit1 != 0 ? exit1 : (exit2 != 0 ? exit2 : (exit3 != 0 ? exit3 : (exit4 != 0 ? exit4 : (exit5 != 0 ? exit5 : (exit6 != 0 ? exit6 : (exit7 != 0 ? exit7 : exit8))))));
                Environment.Exit(finalExit);
                return;
            }

            base.OnStartup(e);
        }
    }
}
