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
        // =========================================
        // フラグ管理
        // =========================================

        // 発注管理フラグ
        // 0 = 表示、2 = 非表示
        private const int VisibleFlag = 0;
        private const int HiddenFlag = 2;

        // 倉庫入庫済フラグ
        // 0 = 未入庫、1 = 入庫済み
        private const int NotReceivedFlag = 0;
        private const int ReceivedFlag = 1;

        // =========================================
        // データ管理（DBなし）
        // =========================================

        // 発注データ
        private readonly List<PurchaseOrderItem> purchaseOrders
            = new List<PurchaseOrderItem>();

        // 入庫データ
        private readonly List<ReceivingItem> receivings
            = new List<ReceivingItem>();

        // 在庫データ
        private readonly List<PurchaseStockItem> stocks
            = new List<PurchaseStockItem>();

        // 編集中の発注明細
        private List<PurchaseOrderDetailItem> editingDetails
            = new List<PurchaseOrderDetailItem>();

        // 編集対象
        private PurchaseOrderItem editingOrder;

        // 新規登録か編集か
        private bool isEditMode;

        // 現在表示している一覧
        // Order = 発注一覧
        // Receiving = 入庫一覧
        // Stock = 在庫一覧
        private string currentView = "Order";

        // =========================================
        // コンストラクタ
        // =========================================

        public PurchaseOrder()
        {
            InitializeComponent();

            LoadSampleData();

            DisplayOrders();
            DisplayReceivings();
            DisplayStocks();

            ShowOrderView();
        }

        // =========================================
        // サンプルデータ
        // =========================================

        private void LoadSampleData()
        {
            purchaseOrders.Clear();
            receivings.Clear();
            stocks.Clear();

            // -------------------------------------
            // 発注サンプル1
            // -------------------------------------

            purchaseOrders.Add(new PurchaseOrderItem
            {
                OrderId = "PO00001",
                SupplierName = "株式会社ABC",
                OrderDate = new DateTime(2026, 10, 1),
                OrderStatus = "発注済み",
                ManagementFlag = VisibleFlag,
                WarehouseReceivedFlag = NotReceivedFlag,

                Details = new List<PurchaseOrderDetailItem>
                {
                    new PurchaseOrderDetailItem
                    {
                        ProductCode = "P00001",
                        ProductName = "ノートパソコン",
                        Quantity = 5,
                        UnitPrice = 10000
                    }
                }
            });

            // -------------------------------------
            // 発注サンプル2
            // -------------------------------------

            purchaseOrders.Add(new PurchaseOrderItem
            {
                OrderId = "PO00002",
                SupplierName = "株式会社DEF",
                OrderDate = new DateTime(2026, 10, 5),
                OrderStatus = "入荷待ち",
                ManagementFlag = VisibleFlag,
                WarehouseReceivedFlag = NotReceivedFlag,

                Details = new List<PurchaseOrderDetailItem>
                {
                    new PurchaseOrderDetailItem
                    {
                        ProductCode = "P00002",
                        ProductName = "マウス",
                        Quantity = 10,
                        UnitPrice = 12000
                    }
                }
            });

            // -------------------------------------
            // 発注サンプル3（入庫済み）
            // -------------------------------------

            purchaseOrders.Add(new PurchaseOrderItem
            {
                OrderId = "PO00003",
                SupplierName = "株式会社GHI",
                OrderDate = new DateTime(2026, 10, 10),
                OrderStatus = "完了",
                ManagementFlag = VisibleFlag,
                WarehouseReceivedFlag = ReceivedFlag,

                Details = new List<PurchaseOrderDetailItem>
                {
                    new PurchaseOrderDetailItem
                    {
                        ProductCode = "P00003",
                        ProductName = "キーボード",
                        Quantity = 5,
                        UnitPrice = 17000
                    }
                }
            });

            // -------------------------------------
            // 発注サンプル4（非表示）
            // -------------------------------------

            purchaseOrders.Add(new PurchaseOrderItem
            {
                OrderId = "PO00004",
                SupplierName = "株式会社JKL",
                OrderDate = new DateTime(2026, 10, 15),
                OrderStatus = "未発注",
                ManagementFlag = HiddenFlag,
                WarehouseReceivedFlag = NotReceivedFlag,

                Details = new List<PurchaseOrderDetailItem>
                {
                    new PurchaseOrderDetailItem
                    {
                        ProductCode = "P00004",
                        ProductName = "モニター",
                        Quantity = 2,
                        UnitPrice = 15000
                    }
                }
            });

            // -------------------------------------
            // 発注明細から在庫データを作成
            // -------------------------------------

            foreach (PurchaseOrderItem order in purchaseOrders)
            {
                UpdateOrderTotal(order);

                if (order.Details == null)
                {
                    continue;
                }

                foreach (PurchaseOrderDetailItem detail in order.Details)
                {
                    EnsureStockItem(
                        detail.ProductCode,
                        detail.ProductName);
                }
            }

            // -------------------------------------
            // サンプルの入庫済み発注を入庫処理
            // -------------------------------------

            PurchaseOrderItem receivedSample =
                purchaseOrders.FirstOrDefault(
                    order => order.OrderId == "PO00003");

            if (receivedSample != null)
            {
                CreateReceivingRecords(receivedSample);
            }
        }

        // =========================================
        // 発注一覧表示
        // =========================================

        private void DisplayOrders()
        {
            if (dg_MainOrders == null)
            {
                return;
            }

            string orderId = txt_MainOrderId.Text.Trim();
            string supplierName = txt_MainSupplierName.Text.Trim();

            string orderStatus = "";

            ComboBoxItem selectedStatus =
                cmb_MainOrderStatus.SelectedItem as ComboBoxItem;

            if (selectedStatus != null)
            {
                orderStatus = selectedStatus.Content.ToString();
            }

            DateTime? orderDate = dp_MainOrderDate.SelectedDate;

            bool includeHidden =
                chk_MainIncludeHidden.IsChecked == true;

            bool includeReceived =
                chk_MainIncludeReceived.IsChecked == true;

            IEnumerable<PurchaseOrderItem> result = purchaseOrders;

            // -------------------------------------
            // 非表示フラグ
            // -------------------------------------

            if (!includeHidden)
            {
                result = result.Where(order =>
                    order.ManagementFlag == VisibleFlag);
            }

            // -------------------------------------
            // 入庫済みフラグ
            // -------------------------------------

            if (!includeReceived)
            {
                result = result.Where(order =>
                    order.WarehouseReceivedFlag == NotReceivedFlag);
            }

            // -------------------------------------
            // 発注番号検索
            // -------------------------------------

            if (!string.IsNullOrWhiteSpace(orderId))
            {
                result = result.Where(order =>
                    order.OrderId.IndexOf(
                        orderId,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // -------------------------------------
            // 仕入先名検索
            // -------------------------------------

            if (!string.IsNullOrWhiteSpace(supplierName))
            {
                result = result.Where(order =>
                    order.SupplierName.IndexOf(
                        supplierName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // -------------------------------------
            // 発注状態検索
            // -------------------------------------

            if (!string.IsNullOrWhiteSpace(orderStatus)
                && orderStatus != "すべて")
            {
                result = result.Where(order =>
                    order.OrderStatus == orderStatus);
            }

            // -------------------------------------
            // 発注日検索
            // -------------------------------------

            if (orderDate.HasValue)
            {
                result = result.Where(order =>
                    order.OrderDate.Date == orderDate.Value.Date);
            }

            // -------------------------------------
            // 表示データ作成
            // -------------------------------------

            List<PurchaseOrderItem> displayList = result
                .OrderByDescending(order => order.OrderDate)
                .ToList();

            dg_MainOrders.ItemsSource = null;
            dg_MainOrders.ItemsSource = displayList;

            if (currentView == "Order")
            {
                txt_MainOrderCount.Text =
                    "表示件数：" + displayList.Count + " 件";
            }

            UpdateSelectionButtons();
        }

        // =========================================
        // 入庫一覧表示
        // =========================================

        private void DisplayReceivings()
        {
            List<ReceivingItem> displayList = receivings
                .OrderByDescending(item => item.ReceivingDate)
                .ThenBy(item => item.ReceivingId)
                .ToList();

            dg_MainReceivings.ItemsSource = null;
            dg_MainReceivings.ItemsSource = displayList;

            if (currentView == "Receiving")
            {
                txt_MainOrderCount.Text =
                    "入庫件数：" + displayList.Count + " 件";
            }
        }

        // =========================================
        // 在庫一覧表示
        // =========================================

        private void DisplayStocks()
        {
            List<PurchaseStockItem> displayList = stocks
                .OrderBy(item => item.ProductCode)
                .ToList();

            dg_MainStocks.ItemsSource = null;
            dg_MainStocks.ItemsSource = displayList;

            if (currentView == "Stock")
            {
                txt_MainOrderCount.Text =
                    "商品件数：" + displayList.Count + " 件";
            }
        }

        // =========================================
        // 在庫データの取得・作成
        // =========================================

        private PurchaseStockItem EnsureStockItem(
            string productCode,
            string productName)
        {
            PurchaseStockItem stock = stocks.FirstOrDefault(item =>
                item.ProductCode.Equals(
                    productCode,
                    StringComparison.OrdinalIgnoreCase));

            if (stock == null)
            {
                stock = new PurchaseStockItem
                {
                    ProductCode = productCode,
                    ProductName = productName,
                    StockQuantity = 0
                };

                stocks.Add(stock);
            }
            else
            {
                stock.ProductName = productName;
            }

            return stock;
        }

        // =========================================
        // 発注一覧に切り替え
        // =========================================

        private void btn_MainShowOrders_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowOrderView();
        }

        private void ShowOrderView()
        {
            currentView = "Order";

            dg_MainOrders.Visibility = Visibility.Visible;
            dg_MainReceivings.Visibility = Visibility.Collapsed;
            dg_MainStocks.Visibility = Visibility.Collapsed;

            btn_MainNewRegister.IsEnabled = true;

            DisplayOrders();
            UpdateSelectionButtons();
        }

        // =========================================
        // 入庫一覧に切り替え
        // =========================================

        private void btn_MainShowReceiving_Click(
            object sender,
            RoutedEventArgs e)
        {
            currentView = "Receiving";

            dg_MainOrders.Visibility = Visibility.Collapsed;
            dg_MainReceivings.Visibility = Visibility.Visible;
            dg_MainStocks.Visibility = Visibility.Collapsed;

            DisableOrderActionButtons();

            DisplayReceivings();
        }

        // =========================================
        // 在庫一覧に切り替え
        // =========================================

        private void btn_MainShowStock_Click(
            object sender,
            RoutedEventArgs e)
        {
            currentView = "Stock";

            dg_MainOrders.Visibility = Visibility.Collapsed;
            dg_MainReceivings.Visibility = Visibility.Collapsed;
            dg_MainStocks.Visibility = Visibility.Visible;

            DisableOrderActionButtons();

            DisplayStocks();
        }

        // =========================================
        // 発注操作ボタンを無効化
        // =========================================

        private void DisableOrderActionButtons()
        {
            btn_MainNewRegister.IsEnabled = false;
            btn_MainEditSelected.IsEnabled = false;
            btn_MainHideSelected.IsEnabled = false;
            btn_MainReceiveSelected.IsEnabled = false;
        }

        // =========================================
        // 検索
        // =========================================

        private void btn_MainSearch_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowOrderView();
        }

        private void SearchField_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ShowOrderView();
            }
        }

        // =========================================
        // 条件クリア
        // =========================================

        private void btn_MainClear_Click(
            object sender,
            RoutedEventArgs e)
        {
            txt_MainOrderId.Clear();
            txt_MainSupplierName.Clear();

            cmb_MainOrderStatus.SelectedIndex = 0;
            dp_MainOrderDate.SelectedDate = null;

            chk_MainIncludeHidden.IsChecked = false;
            chk_MainIncludeReceived.IsChecked = false;

            ShowOrderView();
        }

        private void chk_MainIncludeHidden_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowOrderView();
        }

        private void chk_MainIncludeReceived_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowOrderView();
        }

        // =========================================
        // 新規発注登録
        // =========================================

        private void btn_MainNewRegister_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView != "Order")
            {
                return;
            }

            isEditMode = false;
            editingOrder = null;

            editingDetails =
                new List<PurchaseOrderDetailItem>();

            txt_EditTitle.Text = "発注登録";

            txt_EditSupplierName.Clear();
            dp_EditOrderDate.SelectedDate = DateTime.Today;

            txt_EditProductCode.Clear();
            txt_EditProductName.Clear();
            txt_EditQuantity.Text = "1";
            txt_EditUnitPrice.Clear();

            cmb_EditOrderStatus.SelectedIndex = 0;

            dg_EditDetails.ItemsSource = null;
            dg_EditDetails.ItemsSource = editingDetails;

            UpdateEditTotal();

            txt_EditError.Visibility = Visibility.Collapsed;
            txt_EditError.Text = "";

            btn_EditSave.Content = "登録";

            modal_OrderEdit.Visibility = Visibility.Visible;
        }

        // =========================================
        // 編集ボタン
        // =========================================

        private void btn_MainEditSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView != "Order")
            {
                return;
            }

            OpenEditDialog();
        }

        private void dg_MainOrders_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (currentView != "Order")
            {
                return;
            }

            if (dg_MainOrders.SelectedItem == null)
            {
                return;
            }

            OpenEditDialog();
        }

        // =========================================
        // 編集ダイアログ表示
        // =========================================

        private void OpenEditDialog()
        {
            PurchaseOrderItem selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            if (selectedOrder == null)
            {
                return;
            }

            // 入庫済みは編集不可
            if (selectedOrder.WarehouseReceivedFlag == ReceivedFlag)
            {
                MessageBox.Show(
                    "入庫済みの発注情報は編集できません。",
                    "編集不可",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            isEditMode = true;
            editingOrder = selectedOrder;

            editingDetails = selectedOrder.Details
                .Select(detail => new PurchaseOrderDetailItem
                {
                    ProductCode = detail.ProductCode,
                    ProductName = detail.ProductName,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice
                })
                .ToList();

            txt_EditTitle.Text = "発注情報編集";

            txt_EditSupplierName.Text =
                selectedOrder.SupplierName;

            dp_EditOrderDate.SelectedDate =
                selectedOrder.OrderDate;

            cmb_EditOrderStatus.SelectedIndex =
                GetStatusIndex(selectedOrder.OrderStatus);

            txt_EditProductCode.Clear();
            txt_EditProductName.Clear();
            txt_EditQuantity.Text = "1";
            txt_EditUnitPrice.Clear();

            dg_EditDetails.ItemsSource = null;
            dg_EditDetails.ItemsSource = editingDetails;

            UpdateEditTotal();

            txt_EditError.Visibility = Visibility.Collapsed;
            txt_EditError.Text = "";

            btn_EditSave.Content = "更新";

            modal_OrderEdit.Visibility = Visibility.Visible;
        }

        // =========================================
        // 発注状態のインデックス取得
        // =========================================

        private int GetStatusIndex(string status)
        {
            switch (status)
            {
                case "未発注":
                    return 0;

                case "発注済み":
                    return 1;

                case "入荷待ち":
                    return 2;

                case "完了":
                    return 3;

                default:
                    return 0;
            }
        }

        // =========================================
        // 明細追加
        // =========================================

        private void btn_EditAddDetail_Click(
            object sender,
            RoutedEventArgs e)
        {
            string productCode =
                txt_EditProductCode.Text.Trim();

            string productName =
                txt_EditProductName.Text.Trim();

            int quantity;
            decimal unitPrice;

            if (string.IsNullOrWhiteSpace(productCode))
            {
                ShowEditError("商品コードを入力してください。");
                return;
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                ShowEditError("商品名を入力してください。");
                return;
            }

            if (!int.TryParse(
                txt_EditQuantity.Text.Trim(),
                out quantity)
                || quantity <= 0)
            {
                ShowEditError(
                    "発注数量は1以上の整数で入力してください。");

                return;
            }

            if (!decimal.TryParse(
                txt_EditUnitPrice.Text.Trim(),
                out unitPrice)
                || unitPrice < 0)
            {
                ShowEditError(
                    "単価は0以上の数値で入力してください。");

                return;
            }

            if (editingDetails.Any(detail =>
                detail.ProductCode.Equals(
                    productCode,
                    StringComparison.OrdinalIgnoreCase)))
            {
                ShowEditError(
                    "同じ商品コードの明細は追加できません。");

                return;
            }

            editingDetails.Add(new PurchaseOrderDetailItem
            {
                ProductCode = productCode,
                ProductName = productName,
                Quantity = quantity,
                UnitPrice = unitPrice
            });

            RefreshDetailGrid();

            txt_EditProductCode.Clear();
            txt_EditProductName.Clear();
            txt_EditQuantity.Text = "1";
            txt_EditUnitPrice.Clear();

            txt_EditError.Visibility = Visibility.Collapsed;
            txt_EditError.Text = "";
        }

        // =========================================
        // 明細一覧更新
        // =========================================

        private void RefreshDetailGrid()
        {
            dg_EditDetails.ItemsSource = null;
            dg_EditDetails.ItemsSource = editingDetails;

            UpdateEditTotal();
        }

        // =========================================
        // 明細削除
        // =========================================

        private void btn_EditRemoveDetail_Click(
            object sender,
            RoutedEventArgs e)
        {
            PurchaseOrderDetailItem selectedDetail =
                dg_EditDetails.SelectedItem
                as PurchaseOrderDetailItem;

            if (selectedDetail == null)
            {
                ShowEditError(
                    "削除する明細を選択してください。");

                return;
            }

            editingDetails.Remove(selectedDetail);

            RefreshDetailGrid();

            txt_EditError.Visibility = Visibility.Collapsed;
            txt_EditError.Text = "";
        }

        // =========================================
        // 合計金額計算
        // =========================================

        private void UpdateEditTotal()
        {
            decimal total = editingDetails.Sum(detail =>
                detail.Subtotal);

            txt_EditTotalAmount.Text =
                total.ToString("N0") + " 円";
        }

        private void UpdateOrderTotal(PurchaseOrderItem order)
        {
            if (order.Details == null)
            {
                order.Details =
                    new List<PurchaseOrderDetailItem>();
            }

            order.TotalAmount = order.Details.Sum(detail =>
                detail.Subtotal);
        }

        // =========================================
        // 発注登録・更新
        // =========================================

        private void btn_EditSave_Click(
            object sender,
            RoutedEventArgs e)
        {
            string supplierName =
                txt_EditSupplierName.Text.Trim();

            DateTime? orderDate =
                dp_EditOrderDate.SelectedDate;

            ComboBoxItem selectedStatus =
                cmb_EditOrderStatus.SelectedItem
                as ComboBoxItem;

            if (string.IsNullOrWhiteSpace(supplierName))
            {
                ShowEditError("仕入先名を入力してください。");
                return;
            }

            if (!orderDate.HasValue)
            {
                ShowEditError("発注日を選択してください。");
                return;
            }

            if (editingDetails.Count == 0)
            {
                ShowEditError(
                    "発注商品を1件以上追加してください。");

                return;
            }

            if (selectedStatus == null)
            {
                ShowEditError("発注状態を選択してください。");
                return;
            }

            string status = selectedStatus.Content.ToString();

            // -------------------------------------
            // 更新処理
            // -------------------------------------

            if (isEditMode)
            {
                if (editingOrder == null)
                {
                    ShowEditError(
                        "編集対象の発注情報が見つかりません。");

                    return;
                }

                if (editingOrder.WarehouseReceivedFlag == ReceivedFlag)
                {
                    ShowEditError(
                        "入庫済みの発注情報は編集できません。");

                    return;
                }

                editingOrder.SupplierName = supplierName;
                editingOrder.OrderDate = orderDate.Value;
                editingOrder.OrderStatus = status;

                editingOrder.Details = editingDetails
                    .Select(detail => new PurchaseOrderDetailItem
                    {
                        ProductCode = detail.ProductCode,
                        ProductName = detail.ProductName,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice
                    })
                    .ToList();

                UpdateOrderTotal(editingOrder);

                MessageBox.Show(
                    "発注情報を更新しました。",
                    "更新完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                // ---------------------------------
                // 新規登録処理
                // ---------------------------------

                PurchaseOrderItem newOrder =
                    new PurchaseOrderItem
                    {
                        OrderId = GenerateOrderId(),
                        SupplierName = supplierName,
                        OrderDate = orderDate.Value,
                        OrderStatus = status,
                        ManagementFlag = VisibleFlag,
                        WarehouseReceivedFlag = NotReceivedFlag,

                        Details = editingDetails
                            .Select(detail =>
                                new PurchaseOrderDetailItem
                                {
                                    ProductCode = detail.ProductCode,
                                    ProductName = detail.ProductName,
                                    Quantity = detail.Quantity,
                                    UnitPrice = detail.UnitPrice
                                })
                            .ToList()
                    };

                UpdateOrderTotal(newOrder);

                purchaseOrders.Add(newOrder);

                MessageBox.Show(
                    "発注情報を登録しました。",
                    "登録完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            modal_OrderEdit.Visibility = Visibility.Collapsed;

            ShowOrderView();
        }

        // =========================================
        // 発注番号生成
        // =========================================

        private string GenerateOrderId()
        {
            int maxNumber = 0;

            foreach (PurchaseOrderItem order in purchaseOrders)
            {
                if (order.OrderId != null
                    && order.OrderId.StartsWith("PO")
                    && int.TryParse(
                        order.OrderId.Substring(2),
                        out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }

            return "PO" + (maxNumber + 1).ToString("D5");
        }

        // =========================================
        // エラー表示
        // =========================================

        private void ShowEditError(string message)
        {
            txt_EditError.Text = message;
            txt_EditError.Visibility = Visibility.Visible;
        }

        // =========================================
        // キャンセル
        // =========================================

        private void btn_EditCancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            modal_OrderEdit.Visibility = Visibility.Collapsed;
        }

        private void modal_OrderEdit_Backdrop_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.OriginalSource == modal_OrderEdit)
            {
                modal_OrderEdit.Visibility =
                    Visibility.Collapsed;
            }
        }

        private void EditDialog_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        // =========================================
        // 非表示設定（10.4）
        // =========================================

        private void btn_MainHideSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView != "Order")
            {
                return;
            }

            PurchaseOrderItem selectedOrder =
                dg_MainOrders.SelectedItem
                as PurchaseOrderItem;

            if (selectedOrder == null)
            {
                MessageBox.Show(
                    "非表示にする発注情報を選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (selectedOrder.ManagementFlag == HiddenFlag)
            {
                MessageBox.Show(
                    "この発注情報はすでに非表示です。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "発注番号「" + selectedOrder.OrderId
                + "」を非表示にしますか？",
                "非表示確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // 管理フラグを0から2へ変更
            selectedOrder.ManagementFlag = HiddenFlag;

            MessageBox.Show(
                "発注情報を非表示にしました。",
                "完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DisplayOrders();
        }

        // =========================================
        // 入庫確定（10.5）
        // =========================================

        private void btn_MainReceiveSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView != "Order")
            {
                return;
            }

            PurchaseOrderItem selectedOrder =
                dg_MainOrders.SelectedItem
                as PurchaseOrderItem;

            // -------------------------------------
            // 選択確認
            // -------------------------------------

            if (selectedOrder == null)
            {
                MessageBox.Show(
                    "入庫確定する発注情報を選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -------------------------------------
            // 入庫済み確認
            // -------------------------------------

            if (selectedOrder.WarehouseReceivedFlag == ReceivedFlag)
            {
                MessageBox.Show(
                    "この発注はすでに入庫済みです。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            // -------------------------------------
            // 二重登録確認
            // -------------------------------------

            if (receivings.Any(receiving =>
                receiving.OrderId == selectedOrder.OrderId))
            {
                MessageBox.Show(
                    "この発注番号の入庫レコードはすでに存在します。\n"
                    + "二重入庫を防止するため処理を中止しました。",
                    "入庫確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -------------------------------------
            // 明細確認
            // -------------------------------------

            if (selectedOrder.Details == null
                || selectedOrder.Details.Count == 0)
            {
                MessageBox.Show(
                    "発注明細が存在しないため、入庫確定できません。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -------------------------------------
            // 確認メッセージ
            // -------------------------------------

            MessageBoxResult result = MessageBox.Show(
                "次の発注を入庫確定しますか？\n\n"
                + "発注番号：" + selectedOrder.OrderId + "\n"
                + "仕入先名：" + selectedOrder.SupplierName + "\n"
                + "発注日："
                + selectedOrder.OrderDate.ToString("yyyy/MM/dd") + "\n"
                + "商品明細数："
                + selectedOrder.Details.Count + " 件\n"
                + "合計金額："
                + selectedOrder.TotalAmount.ToString("N0") + " 円\n\n"
                + "入庫レコードを作成し、在庫数量を加算します。\n"
                + "入庫済フラグを0から1に変更します。",
                "入庫確定確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // -------------------------------------
            // 入庫レコード作成・在庫数量加算
            // -------------------------------------

            int createdCount =
                CreateReceivingRecords(selectedOrder);

            if (createdCount == 0)
            {
                MessageBox.Show(
                    "入庫レコードを作成できませんでした。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -------------------------------------
            // 入庫済フラグを0から1へ変更
            // -------------------------------------

            selectedOrder.WarehouseReceivedFlag =
                ReceivedFlag;

            // -------------------------------------
            // 発注状態を完了に変更
            // -------------------------------------

            selectedOrder.OrderStatus = "完了";

            // -------------------------------------
            // 完了メッセージ
            // -------------------------------------

            MessageBox.Show(
                "入庫確定が完了しました。\n\n"
                + "発注番号：" + selectedOrder.OrderId + "\n"
                + "作成した入庫レコード："
                + createdCount + " 件\n"
                + "入庫済フラグ：0 → 1\n"
                + "在庫数量：加算済み",
                "入庫確定完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // -------------------------------------
            // 各一覧を更新
            // -------------------------------------

            DisplayOrders();
            DisplayReceivings();
            DisplayStocks();
        }

        // =========================================
        // 入庫レコード作成・在庫数加算
        // =========================================

        private int CreateReceivingRecords(
            PurchaseOrderItem order)
        {
            if (order == null
                || order.Details == null
                || order.Details.Count == 0)
            {
                return 0;
            }

            // -------------------------------------
            // 二重入庫防止
            // -------------------------------------

            if (receivings.Any(receiving =>
                receiving.OrderId == order.OrderId))
            {
                return 0;
            }

            int count = 0;

            foreach (PurchaseOrderDetailItem detail in order.Details)
            {
                // ---------------------------------
                // 在庫データ取得・作成
                // ---------------------------------

                PurchaseStockItem stock =
                    EnsureStockItem(
                        detail.ProductCode,
                        detail.ProductName);

                // ---------------------------------
                // 入庫レコード作成
                // ---------------------------------

                ReceivingItem receiving = new ReceivingItem
                {
                    ReceivingId = GenerateReceivingId(),
                    OrderId = order.OrderId,
                    ProductCode = detail.ProductCode,
                    ProductName = detail.ProductName,
                    ReceivedQuantity = detail.Quantity,
                    ReceivingDate = DateTime.Today,
                    SupplierName = order.SupplierName
                };

                receivings.Add(receiving);

                // ---------------------------------
                // 在庫数量加算
                // ---------------------------------

                stock.StockQuantity += detail.Quantity;

                count++;
            }

            return count;
        }

        // =========================================
        // 入庫番号生成
        // =========================================

        private string GenerateReceivingId()
        {
            int maxNumber = 0;

            foreach (ReceivingItem receiving in receivings)
            {
                if (receiving.ReceivingId != null
                    && receiving.ReceivingId.StartsWith("RC")
                    && int.TryParse(
                        receiving.ReceivingId.Substring(2),
                        out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }

            return "RC" + (maxNumber + 1).ToString("D5");
        }

        // =========================================
        // 発注一覧の選択変更
        // =========================================

        private void dg_MainOrders_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            UpdateSelectionButtons();
        }

        // =========================================
        // ボタン有効・無効制御
        // =========================================

        private void UpdateSelectionButtons()
        {
            if (dg_MainOrders == null)
            {
                return;
            }

            PurchaseOrderItem selectedOrder =
                dg_MainOrders.SelectedItem
                as PurchaseOrderItem;

            bool hasSelection =
                selectedOrder != null
                && currentView == "Order";

            // 編集ボタン
            if (btn_MainEditSelected != null)
            {
                btn_MainEditSelected.IsEnabled =
                    hasSelection
                    && selectedOrder.WarehouseReceivedFlag
                    != ReceivedFlag;
            }

            // 非表示ボタン
            if (btn_MainHideSelected != null)
            {
                btn_MainHideSelected.IsEnabled =
                    hasSelection;
            }

            // 入庫確定ボタン
            if (btn_MainReceiveSelected != null)
            {
                btn_MainReceiveSelected.IsEnabled =
                    hasSelection
                    && selectedOrder.WarehouseReceivedFlag
                    == NotReceivedFlag;
            }
        }
    }

    // =============================================
    // 発注情報クラス
    // =============================================

    public class PurchaseOrderItem
    {
        // 発注番号
        public string OrderId { get; set; }

        // 仕入先名
        public string SupplierName { get; set; }

        // 発注日
        public DateTime OrderDate { get; set; }

        // 合計金額
        public decimal TotalAmount { get; set; }

        // 発注状態
        public string OrderStatus { get; set; }

        // 管理フラグ：0 = 表示、2 = 非表示
        public int ManagementFlag { get; set; }

        // 倉庫入庫済フラグ：0 = 未入庫、1 = 入庫済み
        public int WarehouseReceivedFlag { get; set; }

        // 発注明細
        public List<PurchaseOrderDetailItem> Details { get; set; }
            = new List<PurchaseOrderDetailItem>();

        // 一覧表示用
        public string WarehouseReceivedText
        {
            get
            {
                return WarehouseReceivedFlag == 1
                    ? "入庫済み"
                    : "未入庫";
            }
        }
    }

    // =============================================
    // 発注明細クラス
    // =============================================

    public class PurchaseOrderDetailItem
    {
        // 商品コード
        public string ProductCode { get; set; }

        // 商品名
        public string ProductName { get; set; }

        // 発注数量
        public int Quantity { get; set; }

        // 単価
        public decimal UnitPrice { get; set; }

        // 小計
        public decimal Subtotal
        {
            get
            {
                return Quantity * UnitPrice;
            }
        }
    }

    // =============================================
    // 入庫情報クラス
    // =============================================

    public class ReceivingItem
    {
        // 入庫番号
        public string ReceivingId { get; set; }

        // 発注番号
        public string OrderId { get; set; }

        // 仕入先名
        public string SupplierName { get; set; }

        // 商品コード
        public string ProductCode { get; set; }

        // 商品名
        public string ProductName { get; set; }

        // 入庫数量
        public int ReceivedQuantity { get; set; }

        // 入庫日
        public DateTime ReceivingDate { get; set; }
    }

    // =============================================
    // 在庫情報クラス
    // =============================================

    public class PurchaseStockItem
    {
        // 商品コード
        public string ProductCode { get; set; }

        // 商品名
        public string ProductName { get; set; }

        // 現在庫数
        public int StockQuantity { get; set; }

        // 在庫状況
        public string StockStatus
        {
            get
            {
                return StockQuantity > 0
                    ? "在庫あり"
                    : "在庫なし";
            }
        }
    }
}