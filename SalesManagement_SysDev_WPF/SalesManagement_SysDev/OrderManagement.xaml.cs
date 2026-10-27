using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class OrderManagement : UserControl
    {
        private readonly List<OrderItem> _orders = new List<OrderItem>();

        private bool _isEditMode;
        private OrderItem _selectedOrder;

        public OrderManagement()
        {
            InitializeComponent();

            LoadSampleOrders();
            ExecuteSearch();
        }

        // =========================================
        // 注文データ（サンプル）
        // =========================================

        private void LoadSampleOrders()
        {
            _orders.Clear();

            _orders.Add(new OrderItem
            {
                OrderId = "ORD0001",
                CustomerName = "株式会社ABC",
                OrderDate = new DateTime(2026, 10, 1),
                OrderStatus = "受付中",
                TotalAmount = 50000,
                ManagementFlag = 0
            });

            _orders.Add(new OrderItem
            {
                OrderId = "ORD0002",
                CustomerName = "株式会社DEF",
                OrderDate = new DateTime(2026, 10, 5),
                OrderStatus = "処理中",
                TotalAmount = 128000,
                ManagementFlag = 0
            });

            _orders.Add(new OrderItem
            {
                OrderId = "ORD0003",
                CustomerName = "株式会社GHI",
                OrderDate = new DateTime(2026, 10, 10),
                OrderStatus = "完了",
                TotalAmount = 75000,
                ManagementFlag = 0
            });

            _orders.Add(new OrderItem
            {
                OrderId = "ORD0004",
                CustomerName = "株式会社JKL",
                OrderDate = new DateTime(2026, 10, 15),
                OrderStatus = "受付中",
                TotalAmount = 36000,
                ManagementFlag = 2
            });
        }

        // =========================================
        // 検索
        // =========================================

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteSearch();
            }
        }

        private void chk_IncludeHidden_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void ExecuteSearch()
        {
            string orderId = txt_SearchOrderId.Text.Trim();
            string customerName = txt_SearchCustomerName.Text.Trim();

            bool includeHidden = chk_IncludeHidden.IsChecked == true;

            string status = "すべて";

            if (cmb_SearchStatus.SelectedItem is ComboBoxItem selectedStatus)
            {
                status = selectedStatus.Content?.ToString() ?? "すべて";
            }

            IEnumerable<OrderItem> query = _orders;

            // 非表示データを除外
            if (!includeHidden)
            {
                query = query.Where(o => o.ManagementFlag == 0);
            }

            // 注文番号検索
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                query = query.Where(o =>
                    o.OrderId.IndexOf(
                        orderId,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 顧客名検索
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                query = query.Where(o =>
                    o.CustomerName.IndexOf(
                        customerName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 注文状態検索
            if (status != "すべて")
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            List<OrderItem> result = query
                .OrderByDescending(o => o.OrderDate)
                .ThenBy(o => o.OrderId)
                .ToList();

            dg_Orders.ItemsSource = null;
            dg_Orders.ItemsSource = result;

            txt_OrderCount.Text = $"表示件数: {result.Count} 件";

            UpdateActionButtons();
        }

        // =========================================
        // 条件クリア
        // =========================================

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchOrderId.Clear();
            txt_SearchCustomerName.Clear();

            cmb_SearchStatus.SelectedIndex = 0;
            chk_IncludeHidden.IsChecked = false;

            dg_Orders.SelectedItem = null;

            ExecuteSearch();
        }

        // =========================================
        // 選択行に応じてボタンを有効化
        // =========================================

        private void dg_Orders_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            _selectedOrder = dg_Orders.SelectedItem as OrderItem;

            bool hasSelection = _selectedOrder != null;

            btn_Update.IsEnabled = hasSelection;

            btn_Hide.IsEnabled =
                hasSelection &&
                _selectedOrder.ManagementFlag == 0;
        }

        // =========================================
        // 注文登録
        // =========================================

        private void btn_Register_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            _selectedOrder = null;

            txt_EditTitle.Text = "注文登録";
            btn_SaveOrder.Content = "注文を登録";

            txt_EditOrderId.Text = GenerateOrderId();
            txt_EditCustomerName.Clear();

            dp_EditOrderDate.SelectedDate = DateTime.Today;
            cmb_EditOrderStatus.SelectedIndex = 0;

            txt_EditTotalAmount.Text = "0";

            HideEditError();

            modal_OrderEdit.Visibility = Visibility.Visible;
        }

        // =========================================
        // 注文更新
        // =========================================

        private void btn_Update_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Orders.SelectedItem is not OrderItem order)
            {
                MessageBox.Show(
                    "更新する注文を選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (order.ManagementFlag != 0)
            {
                MessageBox.Show(
                    "非表示の注文は更新できません。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _isEditMode = true;
            _selectedOrder = order;

            txt_EditTitle.Text = "注文更新";
            btn_SaveOrder.Content = "注文を更新";

            txt_EditOrderId.Text = order.OrderId;
            txt_EditCustomerName.Text = order.CustomerName;

            dp_EditOrderDate.SelectedDate = order.OrderDate;

            SelectStatus(order.OrderStatus);

            txt_EditTotalAmount.Text =
                order.TotalAmount.ToString("0");

            HideEditError();

            modal_OrderEdit.Visibility = Visibility.Visible;
        }

        // =========================================
        // 注文登録・更新の保存
        // =========================================

        private void btn_SaveOrder_Click(object sender, RoutedEventArgs e)
        {
            HideEditError();

            string customerName = txt_EditCustomerName.Text.Trim();

            // 顧客名チェック
            if (string.IsNullOrWhiteSpace(customerName))
            {
                ShowEditError("顧客名を入力してください。");
                txt_EditCustomerName.Focus();
                return;
            }

            // 注文日チェック
            if (dp_EditOrderDate.SelectedDate == null)
            {
                ShowEditError("注文日を選択してください。");
                dp_EditOrderDate.Focus();
                return;
            }

            // 注文状態チェック
            if (cmb_EditOrderStatus.SelectedItem is not ComboBoxItem selectedStatus)
            {
                ShowEditError("注文状態を選択してください。");
                return;
            }

            string status = selectedStatus.Content?.ToString() ?? "";

            // 金額チェック
            if (!decimal.TryParse(
                    txt_EditTotalAmount.Text.Trim(),
                    out decimal totalAmount))
            {
                ShowEditError("合計金額は数値で入力してください。");
                txt_EditTotalAmount.Focus();
                return;
            }

            if (totalAmount < 0)
            {
                ShowEditError("合計金額は0以上で入力してください。");
                txt_EditTotalAmount.Focus();
                return;
            }

            if (_isEditMode)
            {
                // 更新
                if (_selectedOrder == null)
                {
                    ShowEditError("更新対象の注文が見つかりません。");
                    return;
                }

                _selectedOrder.CustomerName = customerName;
                _selectedOrder.OrderDate =
                    dp_EditOrderDate.SelectedDate.Value;

                _selectedOrder.OrderStatus = status;
                _selectedOrder.TotalAmount = totalAmount;
            }
            else
            {
                // 登録
                string newOrderId = GenerateOrderId();

                var newOrder = new OrderItem
                {
                    OrderId = newOrderId,
                    CustomerName = customerName,
                    OrderDate = dp_EditOrderDate.SelectedDate.Value,
                    OrderStatus = status,
                    TotalAmount = totalAmount,
                    ManagementFlag = 0
                };

                _orders.Add(newOrder);
            }

            modal_OrderEdit.Visibility = Visibility.Collapsed;

            ExecuteSearch();

            MessageBox.Show(
                _isEditMode
                    ? "注文情報を更新しました。"
                    : "注文情報を登録しました。",
                "完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================
        // 注文非表示
        // =========================================

        private void btn_Hide_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Orders.SelectedItem is not OrderItem order)
            {
                MessageBox.Show(
                    "非表示にする注文を選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (order.ManagementFlag == 2)
            {
                MessageBox.Show(
                    "この注文はすでに非表示です。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"注文番号「{order.OrderId}」を非表示にしますか？",
                "注文非表示の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // 物理削除せず、管理フラグを 0 → 2 に変更
            order.ManagementFlag = 2;

            ExecuteSearch();

            MessageBox.Show(
                "注文を非表示にしました。",
                "完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================
        // 一覧のダブルクリックで更新画面を開く
        // =========================================

        private void dg_Orders_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (dg_Orders.SelectedItem is OrderItem)
            {
                btn_Update_Click(sender, e);
            }
        }

        // =========================================
        // モーダルを閉じる
        // =========================================

        private void btn_CloseEdit_Click(object sender, RoutedEventArgs e)
        {
            modal_OrderEdit.Visibility = Visibility.Collapsed;
            HideEditError();
        }

        private void modal_OrderEdit_Backdrop_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                modal_OrderEdit.Visibility = Visibility.Collapsed;
                HideEditError();
            }
        }

        // =========================================
        // 注文番号生成
        // =========================================

        private string GenerateOrderId()
        {
            int maxNumber = 0;

            foreach (OrderItem order in _orders)
            {
                string id = order.OrderId;

                if (id.StartsWith("ORD", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(id.Substring(3), out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }

            return $"ORD{maxNumber + 1:0000}";
        }

        // =========================================
        // 注文状態の選択
        // =========================================

        private void SelectStatus(string status)
        {
            for (int i = 0; i < cmb_EditOrderStatus.Items.Count; i++)
            {
                if (cmb_EditOrderStatus.Items[i] is ComboBoxItem item &&
                    item.Content?.ToString() == status)
                {
                    cmb_EditOrderStatus.SelectedIndex = i;
                    return;
                }
            }

            cmb_EditOrderStatus.SelectedIndex = 0;
        }

        // =========================================
        // エラーメッセージ
        // =========================================

        private void ShowEditError(string message)
        {
            txt_EditError.Text = message;
            border_EditError.Visibility = Visibility.Visible;
        }

        private void HideEditError()
        {
            txt_EditError.Text = "";
            border_EditError.Visibility = Visibility.Collapsed;
        }
    }

    // =============================================
    // 注文データモデル（画面確認用）
    // =============================================

    public class OrderItem
    {
        public string OrderId { get; set; } = "";

        public string CustomerName { get; set; } = "";

        public DateTime OrderDate { get; set; }

        public string OrderStatus { get; set; } = "";

        public decimal TotalAmount { get; set; }

        public int ManagementFlag { get; set; }

        public string FlagFormatted
        {
            get
            {
                return ManagementFlag == 2
                    ? "2 (非表示)"
                    : "0 (表示中)";
            }
        }
    }
}