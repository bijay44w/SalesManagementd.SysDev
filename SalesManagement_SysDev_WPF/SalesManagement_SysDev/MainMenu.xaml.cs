using System;
using System.Windows;
using System.Windows.Controls;

namespace SalesManagement_SysDev
{
    public partial class MainMenu : UserControl
    {
        public event Action<string>? NavigationRequested;

        public MainMenu()
        {
            InitializeComponent();
            lbl_LogonDate.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        }

        public void SetEmployeeInfo(MEmployee? employee)
        {
            if (employee != null)
            {
                lbl_User.Text = $"{employee.EmName}";
                lbl_Store.Text = employee.So?.SoName ?? $"店舗 #{employee.SoId}";
                lbl_Role.Text = employee.Po?.PoName ?? "一般";
            }
            else
            {
                lbl_User.Text = "管理者";
                lbl_Store.Text = "本社営業所";
                lbl_Role.Text = "管理者";
            }

            lbl_LogonDate.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        }

        private void btn_Sales_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("07"); // 売上管理
        }

        private void btn_Inventory_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("05"); // 在庫管理
        }

        private void btn_PurchaseOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("10"); // 発注管理
        }

        private void btn_Product_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("04"); // 商品管理
        }

        private void btn_Employee_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("06"); // 社員管理
        }

        private void btn_Customer_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("03"); // 顧客管理
        }

        private void btn_StoreMaster_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("06"); // 店舗・営業所
        }

        private void btn_DivisionMaster_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("06"); // 部署・役職
        }

        private void btn_TaxMaster_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("消費税率は現在 10% (標準税率) / 8% (軽減税率) で設定されています。", "消費税マスター管理", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
