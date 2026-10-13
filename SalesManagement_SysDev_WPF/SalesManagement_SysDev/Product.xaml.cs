using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class Product : UserControl
    {
        public Product()
        {
            InitializeComponent();
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, "商品管理");
        }

        private void card_ProductRegister_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            ShowMessage("商品登録画面");
        }

        private void card_ProductUpdate_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            ShowMessage("商品更新画面");
        }

        private void card_ProductList_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            ShowMessage("商品一覧画面");
        }

        private void card_ProductSearch_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            ShowMessage("商品検索画面");
        }

        private void card_ProductHide_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            ShowMessage("商品非表示画面");
        }
    }
}