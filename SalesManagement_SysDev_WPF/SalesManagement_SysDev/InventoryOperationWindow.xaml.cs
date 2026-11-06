using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public enum InventoryOperationMode
    {
        Register,
        Update,
        List,
        Search,
        Hide
    }

    /// <summary>
    /// 在庫管理の各操作画面。既存プロジェクトの画面構成を増やしすぎないよう、
    /// 操作モードを切り替えて共通の画面を利用する。
    /// </summary>
    public partial class InventoryOperationWindow : Window
    {
        private readonly InventoryOperationMode _mode;
        private readonly StockDataAccess _stockDataAccess = new();

        public InventoryOperationWindow(InventoryOperationMode mode)
        {
            InitializeComponent();
            _mode = mode;
            ConfigureMode();
        }

        private void ConfigureMode()
        {
            switch (_mode)
            {
                case InventoryOperationMode.Register:
                    Title = "在庫登録";
                    TitleText.Text = "在庫登録";
                    DescriptionText.Text = "商品を選択し、初期在庫数を登録します。";
                    SearchPanel.Visibility = Visibility.Collapsed;
                    StockDataGrid.Visibility = Visibility.Collapsed;
                    ProductComboBox.Visibility = Visibility.Visible;
                    ProductLabel.Visibility = Visibility.Visible;
                    ActionButton.Content = "登録";
                    EditPanel.Visibility = Visibility.Visible;
                    LoadProductChoices();
                    break;

                case InventoryOperationMode.Update:
                    Title = "在庫更新";
                    TitleText.Text = "在庫更新";
                    DescriptionText.Text = "一覧から在庫を選択し、在庫数を更新します。";
                    ProductComboBox.Visibility = Visibility.Collapsed;
                    ProductLabel.Visibility = Visibility.Collapsed;
                    ActionButton.Content = "在庫を更新";
                    EditPanel.Visibility = Visibility.Visible;
                    LoadStockData();
                    break;

                case InventoryOperationMode.List:
                    Title = "在庫一覧表示";
                    TitleText.Text = "在庫一覧表示";
                    DescriptionText.Text = "登録されている在庫情報を一覧で表示します。";
                    SearchPanel.Visibility = Visibility.Visible;
                    EditPanel.Visibility = Visibility.Collapsed;
                    LoadStockData();
                    break;

                case InventoryOperationMode.Search:
                    Title = "在庫検索";
                    TitleText.Text = "在庫検索";
                    DescriptionText.Text = "商品ID・商品名・商品コード・型番から在庫を検索します。";
                    SearchPanel.Visibility = Visibility.Visible;
                    EditPanel.Visibility = Visibility.Collapsed;
                    LoadStockData();
                    SearchTextBox.Focus();
                    break;

                case InventoryOperationMode.Hide:
                    Title = "在庫非表示";
                    TitleText.Text = "在庫非表示";
                    DescriptionText.Text = "一覧から対象を選び、在庫を非表示にします。データ自体は削除しません。";
                    SearchPanel.Visibility = Visibility.Visible;
                    EditPanel.Visibility = Visibility.Visible;
                    ProductComboBox.Visibility = Visibility.Collapsed;
                    ProductLabel.Visibility = Visibility.Collapsed;
                    QuantityTextBox.Visibility = Visibility.Collapsed;
                    ActionButton.Content = "選択した在庫を非表示";
                    ActionButton.Width = 190;
                    LoadStockData();
                    break;
            }
        }

        private void LoadProductChoices()
        {
            try
            {
                ProductComboBox.ItemsSource = _stockDataAccess.GetProductChoices();
                if (ProductComboBox.Items.Count > 0)
                {
                    ProductComboBox.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show(
                        "登録対象の商品がありません。先に商品管理で商品を登録してください。",
                        "在庫登録",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("商品一覧を取得できませんでした。データベース接続を確認してください。", ex);
            }
        }

        private void LoadStockData(string? keyword = null)
        {
            try
            {
                StockDataGrid.ItemsSource = _stockDataAccess.GetStockData(keyword);
            }
            catch (Exception ex)
            {
                ShowError("在庫情報を取得できませんでした。データベース接続を確認してください。", ex);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStockData(SearchTextBox.Text);
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            LoadStockData();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadStockData(SearchTextBox.Text);
            }
        }

        private void StockDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mode == InventoryOperationMode.Update &&
                StockDataGrid.SelectedItem is StockDisplayData selected)
            {
                QuantityTextBox.Text = selected.Quantity.ToString();
            }
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (_mode)
                {
                    case InventoryOperationMode.Register:
                        RegisterStock();
                        break;
                    case InventoryOperationMode.Update:
                        UpdateStock();
                        break;
                    case InventoryOperationMode.Hide:
                        HideStock();
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError("在庫処理中にエラーが発生しました。", ex);
            }
        }

        private void RegisterStock()
        {
            if (ProductComboBox.SelectedItem is not StockProductChoice product)
            {
                MessageBox.Show("商品を選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryGetQuantity(out int quantity))
            {
                return;
            }

            if (_stockDataAccess.RegisterStock(product.ProductId, quantity, out string message))
            {
                MessageBox.Show(message, "在庫登録",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                QuantityTextBox.Clear();
            }
            else
            {
                MessageBox.Show(message, "在庫登録",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateStock()
        {
            if (StockDataGrid.SelectedItem is not StockDisplayData selected)
            {
                MessageBox.Show("更新する在庫を一覧から選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryGetQuantity(out int quantity))
            {
                return;
            }

            if (_stockDataAccess.UpdateStock(selected.StockId, quantity, out string message))
            {
                MessageBox.Show(message, "在庫更新",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadStockData();
            }
            else
            {
                MessageBox.Show(message, "在庫更新",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void HideStock()
        {
            if (StockDataGrid.SelectedItem is not StockDisplayData selected)
            {
                MessageBox.Show("非表示にする在庫を一覧から選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var answer = MessageBox.Show(
                $"商品「{selected.ProductName}」の在庫を非表示にしますか？\nデータ自体は削除されません。",
                "在庫非表示の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            if (_stockDataAccess.HideStock(selected.StockId, out string message))
            {
                MessageBox.Show(message, "在庫非表示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadStockData();
            }
            else
            {
                MessageBox.Show(message, "在庫非表示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool TryGetQuantity(out int quantity)
        {
            if (!int.TryParse(QuantityTextBox.Text, out quantity) || quantity < 0)
            {
                MessageBox.Show("在庫数には0以上の整数を入力してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                QuantityTextBox.SelectAll();
                return false;
            }

            return true;
        }

        private static void ShowError(string message, Exception ex)
        {
            MessageBox.Show(
                $"{message}\n詳細: {ex.Message}",
                "在庫管理エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
