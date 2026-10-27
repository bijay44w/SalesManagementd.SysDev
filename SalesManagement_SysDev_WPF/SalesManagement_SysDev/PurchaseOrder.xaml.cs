using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class PurchaseOrder : UserControl
    {
        private List<PurchaseOrderItem> purchaseOrders;

        private PurchaseOrderItem editingOrder;

        private bool isEditMode;

        // 管理フラグ：0 = 表示、2 = 非表示
        private const int VisibleFlag = 0;
        private const int HiddenFlag = 2;

        // =========================================
        // コンストラクター
        // =========================================
        public PurchaseOrder()
        {
            InitializeComponent();

            InitializePurchaseOrders();

            DisplayOrders();

            UpdateMainActionButtonsState();
        }

        // =========================================
        // サンプルデータ
        // =========================================
        private void InitializePurchaseOrders()
        {
            purchaseOrders = new List<PurchaseOrderItem>
            {
                new PurchaseOrderItem
                {
                    OrderId = "PO00001",
                    SupplierName = "株式会社ABC",
                    OrderDate = new DateTime(2026, 10, 1),
                    TotalAmount = 50000,
                    OrderStatus = "発注済み",
                    ManagementFlag = VisibleFlag
                },

                new PurchaseOrderItem
                {
                    OrderId = "PO00002",
                    SupplierName = "株式会社DEF",
                    OrderDate = new DateTime(2026, 10, 5),
                    TotalAmount = 120000,
                    OrderStatus = "入荷待ち",
                    ManagementFlag = VisibleFlag
                },

                new PurchaseOrderItem
                {
                    OrderId = "PO00003",
                    SupplierName = "株式会社GHI",
                    OrderDate = new DateTime(2026, 10, 10),
                    TotalAmount = 85000,
                    OrderStatus = "完了",
                    ManagementFlag = VisibleFlag
                },

                new PurchaseOrderItem
                {
                    OrderId = "PO00004",
                    SupplierName = "株式会社JKL",
                    OrderDate = new DateTime(2026, 10, 15),
                    TotalAmount = 30000,
                    OrderStatus = "未発注",
                    ManagementFlag = HiddenFlag
                }
            };
        }

        // =========================================
        // 発注一覧の表示
        // =========================================
        private void DisplayOrders()
        {
            if (purchaseOrders == null)
            {
                return;
            }

            string orderId = txt_MainOrderId.Text.Trim();
            string supplierName = txt_MainSupplierName.Text.Trim();

            bool includeHidden =
                chk_MainIncludeHidden.IsChecked == true;

            IEnumerable<PurchaseOrderItem> result = purchaseOrders;

            // 発注番号で検索
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                result = result.Where(order =>
                    order.OrderId.IndexOf(
                        orderId,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 仕入先名で検索
            if (!string.IsNullOrWhiteSpace(supplierName))
            {
                result = result.Where(order =>
                    order.SupplierName.IndexOf(
                        supplierName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 非表示データを除外
            if (!includeHidden)
            {
                result = result.Where(order =>
                    order.ManagementFlag == VisibleFlag);
            }

            List<PurchaseOrderItem> displayList =
                result.OrderBy(order => order.OrderId).ToList();

            dg_MainOrders.ItemsSource = displayList;

            txt_MainOrderCount.Text = displayList.Count + "件";

            // 一覧更新後にボタン状態を更新
            UpdateMainActionButtonsState();
        }

        // =========================================
        // 操作ボタンの有効・無効を切り替える
        // =========================================
        private void UpdateMainActionButtonsState()
        {
            bool hasSelection =
                dg_MainOrders.SelectedItem is PurchaseOrderItem;

            btn_MainEditSelected.IsEnabled = hasSelection;
            btn_MainHideSelected.IsEnabled = hasSelection;
        }

        // =========================================
        // 検索ボタン
        // =========================================
        private void btn_MainSearch_Click(
            object sender,
            RoutedEventArgs e)
        {
            DisplayOrders();
        }

        // =========================================
        // Enterキーで検索
        // =========================================
        private void SearchField_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DisplayOrders();
                e.Handled = true;
            }
        }

        // =========================================
        // 一覧表示ボタン
        // =========================================
        private void btn_MainClear_Click(
            object sender,
            RoutedEventArgs e)
        {
            txt_MainOrderId.Clear();
            txt_MainSupplierName.Clear();

            chk_MainIncludeHidden.IsChecked = false;

            dg_MainOrders.SelectedItem = null;

            DisplayOrders();

            UpdateMainActionButtonsState();
        }

        // =========================================
        // 非表示データの表示切り替え
        // =========================================
        private void chk_MainIncludeHidden_Click(
            object sender,
            RoutedEventArgs e)
        {
            DisplayOrders();
        }

        // =========================================
        // 一覧の選択変更
        // =========================================
        private void dg_MainOrders_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            UpdateMainActionButtonsState();
        }

        // =========================================
        // 新規登録ボタン
        // =========================================
        private void btn_MainNewRegister_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenEditDialog(null);
        }

        // =========================================
        // 選択行を編集
        // =========================================
        private void btn_MainEditSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            PurchaseOrderItem selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            if (selectedOrder == null)
            {
                MessageBox.Show(
                    "編集する発注データを選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            OpenEditDialog(selectedOrder);
        }

        // =========================================
        // 登録・更新ダイアログを開く
        // =========================================
        private void OpenEditDialog(PurchaseOrderItem order)
        {
            editingOrder = order;

            txt_EditError.Text = string.Empty;
            txt_EditError.Visibility = Visibility.Collapsed;

            if (order == null)
            {
                // 新規登録
                isEditMode = false;

                txt_EditTitle.Text = "発注登録";
                btn_EditSave.Content = "登録";

                txt_EditSupplierName.Text = string.Empty;

                dp_EditOrderDate.SelectedDate = DateTime.Today;

                txt_EditTotalAmount.Text = string.Empty;

                cmb_EditOrderStatus.SelectedIndex = 0;
            }
            else
            {
                // 更新
                isEditMode = true;

                txt_EditTitle.Text = "発注情報の更新";
                btn_EditSave.Content = "更新";

                txt_EditSupplierName.Text = order.SupplierName;

                dp_EditOrderDate.SelectedDate = order.OrderDate;

                txt_EditTotalAmount.Text =
                    order.TotalAmount.ToString("0");

                SelectOrderStatus(order.OrderStatus);
            }

            modal_OrderEdit.Visibility = Visibility.Visible;
        }

        // =========================================
        // 発注状況を選択
        // =========================================
        private void SelectOrderStatus(string status)
        {
            for (int i = 0; i < cmb_EditOrderStatus.Items.Count; i++)
            {
                ComboBoxItem item =
                    cmb_EditOrderStatus.Items[i] as ComboBoxItem;

                if (item != null &&
                    item.Content != null &&
                    item.Content.ToString() == status)
                {
                    cmb_EditOrderStatus.SelectedIndex = i;
                    return;
                }
            }

            cmb_EditOrderStatus.SelectedIndex = 0;
        }

        // =========================================
        // 登録・更新ボタン
        // =========================================
        private void btn_EditSave_Click(
            object sender,
            RoutedEventArgs e)
        {
            // 仕入先名チェック
            string supplierName =
                txt_EditSupplierName.Text.Trim();

            if (string.IsNullOrWhiteSpace(supplierName))
            {
                ShowEditError("仕入先名を入力してください。");
                txt_EditSupplierName.Focus();
                return;
            }

            // 発注日チェック
            if (dp_EditOrderDate.SelectedDate == null)
            {
                ShowEditError("発注日を選択してください。");
                dp_EditOrderDate.Focus();
                return;
            }

            // 発注金額チェック
            string amountText =
                txt_EditTotalAmount.Text.Trim();

            if (!decimal.TryParse(amountText, out decimal totalAmount))
            {
                ShowEditError(
                    "発注金額は正しい数値で入力してください。");

                txt_EditTotalAmount.Focus();
                return;
            }

            if (totalAmount < 0)
            {
                ShowEditError(
                    "発注金額にマイナスの値は指定できません。");

                txt_EditTotalAmount.Focus();
                return;
            }

            // 発注状況チェック
            ComboBoxItem selectedStatus =
                cmb_EditOrderStatus.SelectedItem as ComboBoxItem;

            if (selectedStatus == null ||
                selectedStatus.Content == null)
            {
                ShowEditError("発注状況を選択してください。");
                cmb_EditOrderStatus.Focus();
                return;
            }

            string orderStatus = selectedStatus.Content.ToString();

            DateTime orderDate =
                dp_EditOrderDate.SelectedDate.Value;

            // 更新処理
            if (isEditMode)
            {
                if (editingOrder == null)
                {
                    ShowEditError(
                        "更新対象の発注データが見つかりません。");

                    return;
                }

                editingOrder.SupplierName = supplierName;
                editingOrder.OrderDate = orderDate;
                editingOrder.TotalAmount = totalAmount;
                editingOrder.OrderStatus = orderStatus;
            }
            else
            {
                // 新規登録
                PurchaseOrderItem newOrder = new PurchaseOrderItem
                {
                    OrderId = GenerateOrderId(),
                    SupplierName = supplierName,
                    OrderDate = orderDate,
                    TotalAmount = totalAmount,
                    OrderStatus = orderStatus,
                    ManagementFlag = VisibleFlag
                };

                purchaseOrders.Add(newOrder);
            }

            bool wasEditMode = isEditMode;

            CloseEditDialog();

            DisplayOrders();

            MessageBox.Show(
                wasEditMode
                    ? "発注情報を更新しました。"
                    : "発注情報を登録しました。",
                "完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================
        // エラーメッセージを表示
        // =========================================
        private void ShowEditError(string message)
        {
            txt_EditError.Text = message;
            txt_EditError.Visibility = Visibility.Visible;
        }

        // =========================================
        // エラーメッセージを非表示
        // =========================================
        private void HideEditError()
        {
            txt_EditError.Text = string.Empty;
            txt_EditError.Visibility = Visibility.Collapsed;
        }

        // =========================================
        // キャンセルボタン
        // =========================================
        private void btn_EditCancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            CloseEditDialog();
        }

        // =========================================
        // ダイアログを閉じる
        // =========================================
        private void CloseEditDialog()
        {
            modal_OrderEdit.Visibility = Visibility.Collapsed;

            HideEditError();

            editingOrder = null;

            dg_MainOrders.SelectedItem = null;

            UpdateMainActionButtonsState();
        }

        // =========================================
        // 背景クリックでダイアログを閉じる
        // =========================================
        private void modal_OrderEdit_Backdrop_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            // 背景そのものがクリックされた場合のみ閉じる
            if (e.OriginalSource == modal_OrderEdit)
            {
                CloseEditDialog();

                e.Handled = true;
            }
        }

        // =========================================
        // ダイアログ内部のクリック
        // =========================================
        private void EditDialog_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            // ダイアログ内の操作では背景側にイベントを伝えない
            e.Handled = true;
        }

        // =========================================
        // 選択行を非表示
        // =========================================
        private void btn_MainHideSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            PurchaseOrderItem selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            if (selectedOrder == null)
            {
                MessageBox.Show(
                    "非表示にする発注データを選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "発注番号「" + selectedOrder.OrderId +
                "」を非表示にしますか？",
                "非表示確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            selectedOrder.ManagementFlag = HiddenFlag;

            dg_MainOrders.SelectedItem = null;

            DisplayOrders();

            UpdateMainActionButtonsState();

            MessageBox.Show(
                "発注データを非表示にしました。",
                "完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================
        // 一覧のダブルクリックで編集
        // =========================================
        private void dg_MainOrders_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            PurchaseOrderItem selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            if (selectedOrder == null)
            {
                return;
            }

            OpenEditDialog(selectedOrder);
        }

        // =========================================
        // 発注番号を自動生成
        // =========================================
        private string GenerateOrderId()
        {
            int maxNumber = 0;

            foreach (PurchaseOrderItem order in purchaseOrders)
            {
                if (string.IsNullOrWhiteSpace(order.OrderId))
                {
                    continue;
                }

                string numberText = order.OrderId.Replace("PO", "");

                if (int.TryParse(numberText, out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }

            return "PO" + (maxNumber + 1).ToString("D5");
        }
    }

    // =============================================
    // 発注データモデル
    // =============================================
    public class PurchaseOrderItem
    {
        public string OrderId { get; set; }

        public string SupplierName { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public int ManagementFlag { get; set; }
    }
}
