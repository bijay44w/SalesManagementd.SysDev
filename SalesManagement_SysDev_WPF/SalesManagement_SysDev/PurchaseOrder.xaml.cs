
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    public partial class PurchaseOrder : UserControl
    {
        // =========================================
        // ログイン社員
        // =========================================

        private readonly MEmployee _loggedInEmployee;

        private int LoggedInEmployeeId => _loggedInEmployee.EmId;

        // =========================================
        // フラグ
        // =========================================

        // 発注管理フラグ
        // 0 = 表示、2 = 非表示
        private const int VisibleFlag = 0;
        private const int HiddenFlag = 2;

        // 入庫済みフラグ
        // 0 = 未入庫、1 = 入庫済み
        private const int NotReceivedFlag = 0;
        private const int ReceivedFlag = 1;

        // 在庫フラグ
        // 0 = 有効、2 = 非表示
        private const int StockVisibleFlag = 0;
        private const int StockHiddenFlag = 2;

        // 入庫フラグ
        // 0 = 有効、2 = 非表示
        private const int WarehousingVisibleFlag = 0;
        private const int WarehousingHiddenFlag = 2;

        // =========================================
        // 画面表示用データ
        // =========================================

        private readonly List<PurchaseOrderItem> purchaseOrders = new();
        private readonly List<ReceivingItem> receivings = new();
        private readonly List<PurchaseStockItem> stocks = new();

        private List<PurchaseOrderDetailItem> editingDetails = new();

        private PurchaseOrderItem editingOrder;

        private bool isEditMode;

        private string currentView = "Order";

        // =========================================
        // コンストラクタ
        // =========================================

        public PurchaseOrder(MEmployee employee)
        {
            InitializeComponent();

            _loggedInEmployee = employee
                ?? throw new ArgumentNullException(nameof(employee));

            // DBに保存できるステータスは
            // WaWarehouseFlagから判断するため、
            // 編集画面のステータス選択は無効にする。
            cmb_EditOrderStatus.IsEnabled = false;

            LoadDatabaseData();

            DisplayOrders();
            DisplayReceivings();
            DisplayStocks();

            ShowOrderView();
        }

        // =========================================
        // DBデータ読み込み
        // =========================================

        private void LoadDatabaseData()
        {
            try
            {
                using (var context = new SalesManagementContext())
                {
                    LoadOrders(context);
                    LoadReceivings(context);
                    LoadStocks(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "発注管理データの読み込みに失敗しました。\n\n"
                    + ex.Message,
                    "DB読み込みエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================
        // 発注データ読み込み
        // =========================================

        private void LoadOrders(SalesManagementContext context)
        {
            purchaseOrders.Clear();

            var data = context.THattyus
                .AsNoTracking()
                .Include(x => x.Ma)
                .Include(x => x.THattyuDetails)
                    .ThenInclude(x => x.Pr)
                .OrderByDescending(x => x.HaDate)
                .ToList();

            foreach (var entity in data)
            {
                var item = new PurchaseOrderItem
                {
                    DatabaseId = entity.HaId,
                    OrderId = FormatOrderId(entity.HaId),

                    EmployeeId = entity.EmId,
                    MaId = entity.MaId,

                    SupplierName = entity.Ma?.MaName ?? "",
                    OrderDate = entity.HaDate,

                    ManagementFlag = entity.HaFlag,

                    WarehouseReceivedFlag =
                        entity.WaWarehouseFlag ?? NotReceivedFlag,

                    OrderStatus =
                        (entity.WaWarehouseFlag ?? NotReceivedFlag)
                        == ReceivedFlag
                            ? "完了"
                            : "入荷待ち"
                };

                foreach (var detail in entity.THattyuDetails)
                {
                    item.Details.Add(
                        new PurchaseOrderDetailItem
                        {
                            DatabaseDetailId = detail.HaDetailId,
                            ProductId = detail.PrId,
                            ProductCode = detail.PrId.ToString(),
                            ProductName = detail.Pr?.PrName ?? "",
                            Quantity = detail.HaQuantity,
                            UnitPrice = detail.Pr?.Price ?? 0
                        });
                }

                UpdateOrderTotal(item);

                purchaseOrders.Add(item);
            }
        }

        // =========================================
        // 入庫データ読み込み
        // =========================================

        private void LoadReceivings(SalesManagementContext context)
        {
            receivings.Clear();

            // WaFlag = 0 の有効な入庫だけを取得
            var data = context.TWarehousings
                .AsNoTracking()
                .Where(x => x.WaFlag == WarehousingVisibleFlag)
                .Include(x => x.Ha)
                    .ThenInclude(x => x.Ma)
                .Include(x => x.TWarehousingDetails)
                    .ThenInclude(x => x.Pr)
                .OrderByDescending(x => x.WaDate)
                .ToList();

            foreach (var entity in data)
            {
                foreach (var detail in entity.TWarehousingDetails)
                {
                    receivings.Add(
                        new ReceivingItem
                        {
                            ReceivingDatabaseId = entity.WaId,
                            ReceivingId = FormatReceivingId(entity.WaId),

                            OrderDatabaseId = entity.HaId,
                            OrderId = FormatOrderId(entity.HaId),

                            EmployeeId = entity.EmId ?? 0,

                            SupplierName = entity.Ha?.Ma?.MaName ?? "",

                            ProductId = detail.PrId,
                            ProductCode = detail.PrId.ToString(),
                            ProductName = detail.Pr?.PrName ?? "",

                            ReceivedQuantity = detail.WaQuantity,
                            ReceivingDate = entity.WaDate
                        });
                }
            }
        }

        // =========================================
        // 在庫データ読み込み
        // =========================================

        private void LoadStocks(SalesManagementContext context)
        {
            stocks.Clear();

            // StFlag = 0 の有効な在庫だけを取得
            var data = context.TStocks
                .AsNoTracking()
                .Where(x => x.StFlag == StockVisibleFlag)
                .Include(x => x.Pr)
                .ToList();

            foreach (var group in data.GroupBy(x => x.PrId))
            {
                var first = group.First();

                stocks.Add(
                    new PurchaseStockItem
                    {
                        ProductId = first.PrId,
                        ProductCode = first.PrId.ToString(),
                        ProductName = first.Pr?.PrName ?? "",

                        // 有効な在庫だけを合計
                        StockQuantity = group.Sum(x => x.StQuantity)
                    });
            }
        }

        // =========================================
        // 発注番号・入庫番号
        // =========================================

        private string FormatOrderId(int id)
        {
            return "PO" + id.ToString("D5");
        }

        private string FormatReceivingId(int id)
        {
            return "RC" + id.ToString("D5");
        }

        // =========================================
        // 商品ID取得
        // =========================================

        private bool TryGetDatabaseId(string code, out int id)
        {
            string value = (code ?? "").Trim();

            if (value.StartsWith(
                "P",
                StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }

            return int.TryParse(value, out id) && id > 0;
        }


        // =========================================
        // 商品コード入力時の商品情報自動取得
        // =========================================

        private void txt_EditProductCode_TextChanged(object sender,TextChangedEventArgs e)
        {
            string productCode = txt_EditProductCode.Text.Trim();

            // 商品コードが空の場合
            if (string.IsNullOrWhiteSpace(productCode))
            {
                txt_EditProductName.Clear();
                txt_EditUnitPrice.Clear();
                return;
            }

            // 商品IDに変換できない場合
            if (!TryGetDatabaseId(productCode, out int productId))
            {
                txt_EditProductName.Clear();
                txt_EditUnitPrice.Clear();
                return;
            }

            try
            {
                using (var context = new SalesManagementContext())
                {
                    // 商品マスタから商品情報を取得
                    var product = context.MProducts
                        .AsNoTracking()
                        .FirstOrDefault(x =>
                            x.PrId == productId
                            && x.PrFlag == VisibleFlag);

                    // 商品が存在しない、または非表示の場合
                    if (product == null)
                    {
                        txt_EditProductName.Clear();
                        txt_EditUnitPrice.Clear();
                        return;
                    }

                    // 商品名を自動表示
                    txt_EditProductName.Text = product.PrName;

                    // 単価を自動表示
                    txt_EditUnitPrice.Text = product.Price.ToString("N0");
                }
            }
            catch (Exception ex)
            {
                txt_EditProductName.Clear();
                txt_EditUnitPrice.Clear();

                ShowEditError(
                    "商品情報の取得に失敗しました。\n"
                    + ex.Message);
            }
        }

        // =========================================
        // ログイン社員の存在確認
        // =========================================

        private bool IsLoggedInEmployeeValid(
            SalesManagementContext context)
        {
            return context.MEmployees.Any(
                x => x.EmId == LoggedInEmployeeId);
        }

        // =========================================
        // 発注一覧表示
        // =========================================

        private void DisplayOrders()
        {
            if (dg_MainOrders == null)
                return;

            string orderId = txt_MainOrderId.Text.Trim();
            string supplierName = txt_MainSupplierName.Text.Trim();

            string orderStatus = "";

            ComboBoxItem selectedStatus =
                cmb_MainOrderStatus.SelectedItem as ComboBoxItem;

            if (selectedStatus != null)
            {
                orderStatus =
                    selectedStatus.Content?.ToString() ?? "";
            }

            DateTime? orderDate = dp_MainOrderDate.SelectedDate;

            bool includeHidden =
                chk_MainIncludeHidden.IsChecked == true;

            bool includeReceived =
                chk_MainIncludeReceived.IsChecked == true;

            IEnumerable<PurchaseOrderItem> result = purchaseOrders;

            // 非表示を除外
            if (!includeHidden)
            {
                result = result.Where(
                    x => x.ManagementFlag == VisibleFlag);
            }

            // 入庫済みを除外
            if (!includeReceived)
            {
                result = result.Where(
                    x => x.WarehouseReceivedFlag == NotReceivedFlag);
            }

            // 発注番号検索
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                result = result.Where(
                    x => x.OrderId.IndexOf(
                        orderId,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 仕入先名検索
            if (!string.IsNullOrWhiteSpace(supplierName))
            {
                result = result.Where(
                    x => x.SupplierName.IndexOf(
                        supplierName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // ステータス検索
            if (!string.IsNullOrWhiteSpace(orderStatus)
                && orderStatus != "すべて")
            {
                result = result.Where(
                    x => x.OrderStatus == orderStatus);
            }

            // 発注日検索
            if (orderDate.HasValue)
            {
                result = result.Where(
                    x => x.OrderDate.Date == orderDate.Value.Date);
            }

            var displayList = result
                .OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.DatabaseId)
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
            var displayList = receivings
                .OrderByDescending(x => x.ReceivingDate)
                .ThenByDescending(x => x.ReceivingDatabaseId)
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
            var displayList = stocks
                .OrderBy(x => x.ProductCode)
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
            var home = Window.GetWindow(this) as F_Home;
            if (home != null)
            {
                home.NavigateToTag("11"); // 入庫管理画面へ遷移
                return;
            }

            currentView = "Receiving";

            dg_MainOrders.Visibility = Visibility.Collapsed;
            dg_MainReceivings.Visibility = Visibility.Visible;
            dg_MainStocks.Visibility = Visibility.Collapsed;

            DisableOrderActionButtons();

            DisplayReceivings();
        }

        // =========================================
        // 在庫一覧（在庫管理へ遷移）
        // =========================================

        private void btn_MainShowStock_Click(
            object sender,
            RoutedEventArgs e)
        {
            var home = Window.GetWindow(this) as F_Home;
            if (home != null)
            {
                home.NavigateToTag("05"); // 在庫管理画面へ遷移
                return;
            }

            currentView = "Stock";

            dg_MainOrders.Visibility = Visibility.Collapsed;
            dg_MainReceivings.Visibility = Visibility.Collapsed;
            dg_MainStocks.Visibility = Visibility.Visible;

            DisableOrderActionButtons();

            DisplayStocks();
        }

        private void DisableOrderActionButtons()
        {
            btn_MainNewRegister.IsEnabled = false;
            btn_MainEditSelected.IsEnabled = false;
            btn_MainHideSelected.IsEnabled = false;
            if (btn_MainDeleteSelected != null)
                btn_MainDeleteSelected.IsEnabled = false;
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
        // 検索条件クリア
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
                return;

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

            // 0=未入庫、1=入庫済みではなく、
            // 表示上の「入荷待ち」を選択
            cmb_EditOrderStatus.SelectedIndex = 2;

            dg_EditDetails.ItemsSource = null;
            dg_EditDetails.ItemsSource = editingDetails;

            UpdateEditTotal();

            txt_EditError.Visibility = Visibility.Collapsed;
            txt_EditError.Text = "";

            btn_EditSave.Content = "登録";

            modal_OrderEdit.Visibility = Visibility.Visible;
        }

        // =========================================
        // 編集
        // =========================================

        private void btn_MainEditSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView == "Order")
            {
                OpenEditDialog();
            }
        }

        private void dg_MainOrders_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (currentView == "Order"
                && dg_MainOrders.SelectedItem is PurchaseOrderItem selectedOrder)
            {
                OpenDetailModal(selectedOrder);
            }
        }

        private void OpenEditDialog()
        {
            var selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            if (selectedOrder == null)
                return;

            if (selectedOrder.ManagementFlag != VisibleFlag)
            {
                MessageBox.Show(
                    "非表示の発注情報は編集できません。",
                    "編集不可",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

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
                .Select(x => new PurchaseOrderDetailItem
                {
                    DatabaseDetailId = x.DatabaseDetailId,
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice
                })
                .ToList();

            txt_EditTitle.Text = "発注情報編集";

            txt_EditSupplierName.Text =
                selectedOrder.SupplierName;

            dp_EditOrderDate.SelectedDate =
                selectedOrder.OrderDate;

            cmb_EditOrderStatus.SelectedIndex =
                selectedOrder.WarehouseReceivedFlag == ReceivedFlag
                    ? 3
                    : 2;

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
        // 商品明細追加
        // =========================================

        private void btn_EditAddDetail_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!TryGetDatabaseId(
                txt_EditProductCode.Text,
                out int productId))
            {
                ShowEditError(
                    "商品コードには商品IDを入力してください。");

                return;
            }

            if (!int.TryParse(
                txt_EditQuantity.Text.Trim(),
                out int quantity)
                || quantity <= 0)
            {
                ShowEditError(
                    "発注数量は1以上の整数で入力してください。");

                return;
            }

            if (editingDetails.Any(
                x => x.ProductId == productId))
            {
                ShowEditError(
                    "同じ商品は重複して追加できません。");

                return;
            }

            try
            {
                using (var context = new SalesManagementContext())
                {
                    var product = context.MProducts
                        .AsNoTracking()
                        .FirstOrDefault(x =>
                            x.PrId == productId
                            && x.PrFlag == VisibleFlag);

                    if (product == null)
                    {
                        ShowEditError(
                            "指定された商品が見つからないか、非表示です。");

                        return;
                    }

                    editingDetails.Add(
                        new PurchaseOrderDetailItem
                        {
                            ProductId = product.PrId,
                            ProductCode = product.PrId.ToString(),
                            ProductName = product.PrName,
                            Quantity = quantity,
                            UnitPrice = product.Price
                        });
                }

                RefreshDetailGrid();

                txt_EditProductCode.Clear();
                txt_EditProductName.Clear();
                txt_EditQuantity.Text = "1";
                txt_EditUnitPrice.Clear();

                txt_EditError.Visibility = Visibility.Collapsed;
                txt_EditError.Text = "";
            }
            catch (Exception ex)
            {
                ShowEditError(
                    "商品情報の取得に失敗しました。\n"
                    + ex.Message);
            }
        }

        private void RefreshDetailGrid()
        {
            dg_EditDetails.ItemsSource = null;
            dg_EditDetails.ItemsSource = editingDetails;

            UpdateEditTotal();
        }

        // =========================================
        // 商品明細削除
        // =========================================

        private void btn_EditRemoveDetail_Click(
            object sender,
            RoutedEventArgs e)
        {
            var selectedDetail =
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
        // 合計金額
        // =========================================

        private void UpdateEditTotal()
        {
            decimal total = editingDetails.Sum(
                x => x.Subtotal);

            txt_EditTotalAmount.Text =
                total.ToString("N0") + " 円";
        }

        private void UpdateOrderTotal(
            PurchaseOrderItem order)
        {
            if (order.Details == null)
            {
                order.Details =
                    new List<PurchaseOrderDetailItem>();
            }

            order.TotalAmount =
                order.Details.Sum(x => x.Subtotal);
        }

        // =========================================
        // 仕入先取得
        // =========================================

        private bool TryGetMakerId(
            SalesManagementContext context,
            string makerName,
            out int makerId)
        {
            makerId = 0;

            string name = (makerName ?? "").Trim();

            var maker = context.MMakers
                .FirstOrDefault(x =>
                    x.MaName == name
                    && x.MaFlag == VisibleFlag);

            if (maker == null)
                return false;

            makerId = maker.MaId;

            return true;
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

            if (editingDetails.Any(x => x.Quantity <= 0))
            {
                ShowEditError(
                    "発注数量は1以上にしてください。");

                return;
            }

            try
            {
                using (var context = new SalesManagementContext())
                using (var transaction =
                    context.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    // ログイン社員確認
                    if (!IsLoggedInEmployeeValid(context))
                    {
                        ShowEditError(
                            "ログイン中の社員情報がDBに存在しません。");

                        return;
                    }

                    // 仕入先確認
                    if (!TryGetMakerId(
                        context,
                        supplierName,
                        out int makerId))
                    {
                        ShowEditError(
                            "仕入先マスタに存在する仕入先名を入力してください。");

                        return;
                    }

                    // 商品情報をDBで再確認
                    foreach (var detail in editingDetails)
                    {
                        var product = context.MProducts
                            .FirstOrDefault(x =>
                                x.PrId == detail.ProductId
                                && x.PrFlag == VisibleFlag);

                        if (product == null)
                        {
                            ShowEditError(
                                "商品ID「"
                                + detail.ProductId
                                + "」が見つからないか、非表示です。");

                            return;
                        }

                        // 商品マスタの最新情報を反映
                        detail.ProductName = product.PrName;
                        detail.UnitPrice = product.Price;
                    }

                    if (isEditMode)
                    {
                        if (editingOrder == null)
                        {
                            ShowEditError(
                                "編集対象が見つかりません。");

                            return;
                        }

                        var entity = context.THattyus
                            .Include(x => x.THattyuDetails)
                            .FirstOrDefault(x =>
                                x.HaId == editingOrder.DatabaseId);

                        if (entity == null)
                        {
                            ShowEditError(
                                "DB上の発注情報が見つかりません。");

                            return;
                        }

                        if (entity.HaFlag != VisibleFlag)
                        {
                            ShowEditError(
                                "非表示の発注情報は編集できません。");

                            return;
                        }

                        if ((entity.WaWarehouseFlag
                            ?? NotReceivedFlag) == ReceivedFlag)
                        {
                            ShowEditError(
                                "入庫済みの発注情報は編集できません。");

                            return;
                        }

                        entity.MaId = makerId;
                        entity.HaDate = orderDate.Value;

                        // 既存明細を削除
                        context.THattyuDetails.RemoveRange(
                            entity.THattyuDetails.ToList());

                        entity.THattyuDetails.Clear();

                        // 新しい明細を登録
                        foreach (var detail in editingDetails)
                        {
                            entity.THattyuDetails.Add(
                                new THattyuDetail
                                {
                                    PrId = detail.ProductId,
                                    HaQuantity = detail.Quantity
                                });
                        }
                    }
                    else
                    {
                        var entity = new THattyu
                        {
                            MaId = makerId,

                            // ログイン中の社員ID
                            EmId = LoggedInEmployeeId,

                            HaDate = orderDate.Value,

                            WaWarehouseFlag = NotReceivedFlag,

                            HaFlag = VisibleFlag,
                            HaHidden = null
                        };

                        foreach (var detail in editingDetails)
                        {
                            entity.THattyuDetails.Add(
                                new THattyuDetail
                                {
                                    PrId = detail.ProductId,
                                    HaQuantity = detail.Quantity
                                });
                        }

                        context.THattyus.Add(entity);
                    }

                    context.SaveChanges();

                    transaction.Commit();
                }

                MessageBox.Show(
                    isEditMode
                        ? "発注情報を更新しました。"
                        : "発注情報を登録しました。\n登録社員ID："
                            + LoggedInEmployeeId,
                    isEditMode ? "更新完了" : "登録完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                modal_OrderEdit.Visibility =
                    Visibility.Collapsed;

                ReloadAndDisplay();
            }
            catch (Exception ex)
            {
                ShowEditError(
                    "発注情報の保存に失敗しました。\n"
                    + ex.Message);
            }
        }

        // =========================================
        // 再読み込み
        // =========================================

        private void ReloadAndDisplay()
        {
            LoadDatabaseData();

            DisplayOrders();
            DisplayReceivings();
            DisplayStocks();

            UpdateSelectionButtons();
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
            modal_OrderEdit.Visibility =
                Visibility.Collapsed;
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
        // 発注非表示
        // =========================================

        private void btn_MainHideSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView != "Order")
                return;

            var selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

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

            var result = MessageBox.Show(
                "発注番号「"
                + selectedOrder.OrderId
                + "」を非表示にしますか？",
                "非表示確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var context = new SalesManagementContext())
                {
                    var entity = context.THattyus
                        .FirstOrDefault(x =>
                            x.HaId == selectedOrder.DatabaseId);

                    if (entity == null)
                    {
                        MessageBox.Show(
                            "発注情報が見つかりません。");

                        return;
                    }

                    if (entity.HaFlag != VisibleFlag)
                    {
                        MessageBox.Show(
                            "この発注情報はすでに非表示です。");

                        return;
                    }

                    entity.HaFlag = HiddenFlag;

                    context.SaveChanges();
                }

                MessageBox.Show(
                    "発注情報を非表示にしました。",
                    "完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ReloadAndDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "非表示処理に失敗しました。\n"
                    + ex.Message,
                    "DBエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btn_MainDeleteSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView != "Order")
                return;

            var selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            if (selectedOrder == null)
            {
                MessageBox.Show(
                    "削除する発注情報を選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"次の発注データを完全に削除しますか？\n\n" +
                $"発注番号：{selectedOrder.OrderId}\n" +
                $"仕入先名：{selectedOrder.SupplierName}\n" +
                $"発注金額：{selectedOrder.TotalAmount:N0} 円\n\n" +
                "この操作は取り消せません。",
                "発注の完全削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var context = new SalesManagementContext();
                var entity = context.THattyus
                    .Include(h => h.THattyuDetails)
                    .FirstOrDefault(x => x.HaId == selectedOrder.DatabaseId);

                if (entity == null)
                {
                    MessageBox.Show("発注情報が見つかりません。");
                    ReloadAndDisplay();
                    return;
                }

                // 関連入庫データがあるか確認
                bool hasReceiving = context.TWarehousings.Any(w => w.HaId == entity.HaId) ||
                                    entity.WaWarehouseFlag == 1;
                if (hasReceiving)
                {
                    MessageBox.Show(
                        "この発注には入庫データが存在するため、完全削除できません。\n非表示機能を使用してください。",
                        "削除できません",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (entity.THattyuDetails != null && entity.THattyuDetails.Any())
                {
                    context.THattyuDetails.RemoveRange(entity.THattyuDetails);
                }

                context.THattyus.Remove(entity);
                context.SaveChanges();

                MessageBox.Show(
                    $"発注番号「{selectedOrder.OrderId}」を完全に削除しました。",
                    "完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ReloadAndDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "発注情報の削除中にエラーが発生しました。\n" + ex.Message,
                    "DBエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================
        // 入庫確定
        // =========================================

        private void btn_MainReceiveSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentView != "Order")
                return;

            var selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            if (selectedOrder == null)
            {
                MessageBox.Show(
                    "入庫確定する発注情報を選択してください。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (selectedOrder.ManagementFlag != VisibleFlag)
            {
                MessageBox.Show(
                    "非表示の発注情報は入庫確定できません。",
                    "入庫不可",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (selectedOrder.WarehouseReceivedFlag == ReceivedFlag)
            {
                MessageBox.Show(
                    "この発注はすでに入庫済みです。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (selectedOrder.Details == null
                || selectedOrder.Details.Count == 0)
            {
                MessageBox.Show(
                    "発注明細が存在しません。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = MessageBox.Show(
                "次の発注を入庫確定しますか？\n\n"
                + "発注番号："
                + selectedOrder.OrderId + "\n"
                + "仕入先名："
                + selectedOrder.SupplierName + "\n"
                + "発注日："
                + selectedOrder.OrderDate.ToString("yyyy/MM/dd") + "\n"
                + "明細数："
                + selectedOrder.Details.Count + " 件\n\n"
                + "入庫データを登録し、在庫数量を加算します。",
                "入庫確定確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var context = new SalesManagementContext())
                using (var transaction =
                    context.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    // ログイン社員確認
                    if (!IsLoggedInEmployeeValid(context))
                    {
                        MessageBox.Show(
                            "ログイン中の社員情報がDBに存在しません。",
                            "社員情報エラー",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    // 発注情報を再取得
                    var order = context.THattyus
                        .Include(x => x.THattyuDetails)
                        .FirstOrDefault(x =>
                            x.HaId == selectedOrder.DatabaseId);

                    if (order == null)
                    {
                        MessageBox.Show(
                            "DB上の発注情報が見つかりません。");

                        return;
                    }

                    // 非表示の発注は入庫不可
                    if (order.HaFlag != VisibleFlag)
                    {
                        MessageBox.Show(
                            "非表示の発注情報は入庫確定できません。");

                        return;
                    }

                    // 入庫済み確認
                    if ((order.WaWarehouseFlag
                        ?? NotReceivedFlag) == ReceivedFlag)
                    {
                        MessageBox.Show(
                            "この発注はすでに入庫済みです。");

                        return;
                    }

                    if (order.THattyuDetails == null
                        || order.THattyuDetails.Count == 0)
                    {
                        MessageBox.Show(
                            "発注明細が存在しません。");

                        return;
                    }

                    if (order.THattyuDetails.Any(
                        x => x.HaQuantity <= 0))
                    {
                        MessageBox.Show(
                            "発注明細に不正な数量があります。");

                        return;
                    }

                    // 有効・非表示を問わず、
                    // 入庫履歴が存在する場合は二重入庫を防止する。
                    bool alreadyReceived =
                        context.TWarehousings.Any(
                            x => x.HaId == order.HaId);

                    if (alreadyReceived)
                    {
                        MessageBox.Show(
                            "この発注の入庫データがすでに存在します。\n"
                            + "二重入庫を防ぐため処理を中止しました。",
                            "二重入庫防止",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    // 入庫ヘッダー
                    var warehousing = new TWarehousing
                    {
                        HaId = order.HaId,

                        // 入庫担当社員ID
                        EmId = LoggedInEmployeeId,

                        WaDate = DateTime.Today,

                        WaShelfFlag = 0,

                        WaHidden = null,

                        // 0 = 有効
                        WaFlag = WarehousingVisibleFlag
                    };

                    // 同一商品の明細が複数存在しても
                    // 商品ごとに数量をまとめて在庫へ加算する。
                    var groupedDetails = order.THattyuDetails
                        .GroupBy(x => x.PrId)
                        .Select(g => new
                        {
                            ProductId = g.Key,
                            Quantity = g.Sum(x => x.HaQuantity)
                        })
                        .ToList();

                    foreach (var detail in groupedDetails)
                    {
                        // 商品がDBに存在するか確認
                        var productExists =
                            context.MProducts.Any(
                                x => x.PrId == detail.ProductId);

                        if (!productExists)
                        {
                            MessageBox.Show(
                                "商品ID「"
                                + detail.ProductId
                                + "」が商品マスタに存在しません。");

                            return;
                        }

                        // 有効な在庫レコードだけを取得
                        var stock = context.TStocks
                            .FirstOrDefault(x =>
                                x.PrId == detail.ProductId
                                && x.StFlag == StockVisibleFlag);

                        if (stock == null)
                        {
                            // 有効な在庫が存在しない場合は新規作成
                            stock = new TStock
                            {
                                PrId = detail.ProductId,
                                StQuantity = 0,

                                // 0 = 有効
                                StFlag = StockVisibleFlag
                            };

                            context.TStocks.Add(stock);
                        }

                        // 在庫数量を加算
                        stock.StQuantity = checked(
                            stock.StQuantity + detail.Quantity);

                        // 入庫明細
                        warehousing.TWarehousingDetails.Add(
                            new TWarehousingDetail
                            {
                                PrId = detail.ProductId,
                                WaQuantity = detail.Quantity
                            });
                    }

                    // 発注を入庫済みに変更
                    order.WaWarehouseFlag = ReceivedFlag;

                    // 入庫データを追加
                    context.TWarehousings.Add(warehousing);

                    // DB保存
                    context.SaveChanges();

                    // トランザクション確定
                    transaction.Commit();
                }

                MessageBox.Show(
                    "入庫確定が完了しました。\n\n"
                    + "発注番号："
                    + selectedOrder.OrderId + "\n"
                    + "入庫担当社員ID："
                    + LoggedInEmployeeId + "\n"
                    + "入庫データ登録済み\n"
                    + "在庫数量加算済み",
                    "入庫確定完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ReloadAndDisplay();
            }
            catch (OverflowException)
            {
                MessageBox.Show(
                    "在庫数量が上限を超えるため、入庫処理を中止しました。",
                    "数量エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "入庫処理に失敗しました。\n\n"
                    + ex.Message,
                    "DBエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
                return;

            var selectedOrder =
                dg_MainOrders.SelectedItem as PurchaseOrderItem;

            bool hasSelection =
                selectedOrder != null
                && currentView == "Order";

            bool isVisible =
                hasSelection
                && selectedOrder.ManagementFlag == VisibleFlag;

            bool isNotReceived =
                hasSelection
                && selectedOrder.WarehouseReceivedFlag
                    == NotReceivedFlag;

            if (btn_MainDetailSelected != null)
            {
                btn_MainDetailSelected.IsEnabled = hasSelection;
            }

            if (btn_MainEditSelected != null)
            {
                btn_MainEditSelected.IsEnabled =
                    isVisible && isNotReceived;
            }

            if (btn_MainHideSelected != null)
            {
                btn_MainHideSelected.IsEnabled =
                    isVisible;
            }

            if (btn_MainDeleteSelected != null)
            {
                btn_MainDeleteSelected.IsEnabled =
                    isVisible;
            }

            if (btn_MainReceiveSelected != null)
            {
                btn_MainReceiveSelected.IsEnabled =
                    isVisible && isNotReceived;
            }
        }

        // =============================================
        // 発注詳細モーダル表示 (T_HattyuDetail)
        // =============================================

        private void btn_MainDetailSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = dg_MainOrders.SelectedItem as PurchaseOrderItem;
            if (selectedOrder != null)
            {
                OpenDetailModal(selectedOrder);
            }
        }

        private void OpenDetailModal(PurchaseOrderItem order)
        {
            if (order == null) return;

            detail_txt_OrderId.Text = $"発注番号: {order.OrderId} (ID: {order.DatabaseId})";
            detail_txt_Supplier.Text = $"仕入先: {order.SupplierName}";
            detail_txt_Date.Text = $"発注日: {order.OrderDate:yyyy/MM/dd}";
            detail_txt_Status.Text = $"状態: {order.OrderStatus} (合計: {order.TotalAmount:N0} 円)";

            try
            {
                using var context = new SalesManagementContext();
                var details = context.THattyuDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.HaId == order.DatabaseId)
                    .Select(d => new HattyuDetailDisplayModel
                    {
                        HaDetailId = d.HaDetailId,
                        HaId = d.HaId,
                        PrId = d.PrId,
                        PrName = d.Pr != null ? d.Pr.PrName : $"商品ID: {d.PrId}",
                        HaQuantity = d.HaQuantity
                    })
                    .ToList();

                dg_HattyuDetailItems.ItemsSource = details;
                modal_Detail.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"発注詳細の取得に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_CloseDetail_Click(object sender, RoutedEventArgs e)
        {
            modal_Detail.Visibility = Visibility.Collapsed;
        }

        private void modal_Detail_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_Detail.Visibility = Visibility.Collapsed;
        }
    }

    // =============================================
    // 発注詳細表示モデル (T_HattyuDetail)
    // =============================================

    public class HattyuDetailDisplayModel
    {
        public int HaDetailId { get; set; }
        public int HaId { get; set; }
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int HaQuantity { get; set; }
    }

    // =============================================
    // 発注情報
    // =============================================

    public class PurchaseOrderItem
    {
        public int DatabaseId { get; set; }

        public int EmployeeId { get; set; }

        public int MaId { get; set; }

        public string OrderId { get; set; }

        public string SupplierName { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public int ManagementFlag { get; set; }

        public int WarehouseReceivedFlag { get; set; }

        public List<PurchaseOrderDetailItem> Details { get; set; }
            = new List<PurchaseOrderDetailItem>();

        public string WarehouseReceivedText =>
            WarehouseReceivedFlag == 1
                ? "入庫済み"
                : "未入庫";
    }

    // =============================================
    // 発注明細
    // =============================================

    public class PurchaseOrderDetailItem
    {
        public int DatabaseDetailId { get; set; }

        public int ProductId { get; set; }

        public string ProductCode { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Subtotal =>
            Quantity * UnitPrice;
    }

    // =============================================
    // 入庫情報
    // =============================================

    public class ReceivingItem
    {
        public int ReceivingDatabaseId { get; set; }

        public int OrderDatabaseId { get; set; }

        public int EmployeeId { get; set; }

        public int ProductId { get; set; }

        public string ReceivingId { get; set; }

        public string OrderId { get; set; }

        public string SupplierName { get; set; }

        public string ProductCode { get; set; }

        public string ProductName { get; set; }

        public int ReceivedQuantity { get; set; }

        public DateTime ReceivingDate { get; set; }
    }

    // =============================================
    // 在庫情報
    // =============================================

    public class PurchaseStockItem
    {
        public int ProductId { get; set; }

        public string ProductCode { get; set; }

        public string ProductName { get; set; }

        public int StockQuantity { get; set; }

        public string StockStatus =>
            StockQuantity > 0
                ? "在庫あり"
                : "在庫なし";
    }
}
