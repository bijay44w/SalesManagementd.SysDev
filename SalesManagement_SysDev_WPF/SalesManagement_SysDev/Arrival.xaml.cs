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

namespace SalesManagement_SysDev
{
    /// <summary>
    /// Arrival.xaml の相互作用ロジック
    /// </summary>
    public partial class Arrival : UserControl
    {
        public Arrival()
        {
            InitializeComponent();
        }

        private void card_ArrivalDelete_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(
                "入荷情報の削除機能です。\n安全なデータ管理のため、関連する後続処理があるデータは非表示機能をご利用ください。",
                "入荷削除",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
