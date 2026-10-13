
using System;
using System.Windows.Controls;

namespace SalesManagement_SysDev
{
    public partial class Customer : UserControl
    {
        // 顧客登録画面を開くように F_Home に通知する
        public event EventHandler ClientRegisterRequested;

        public Customer()
        {
            InitializeComponent();
        }

        // 顧客登録カードがクリックされたとき
        private void card_ClientRegister_MouseLeftButtonUp(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            ClientRegisterRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
