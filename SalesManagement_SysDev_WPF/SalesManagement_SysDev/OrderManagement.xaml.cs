
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 注文管理画面
    /// ・注文一覧、検索
    /// ・注文登録、複数商品編集
    /// ・注文非表示
    /// ・注文確定、在庫減算
    /// ・出荷管理へのデータ登録
    /// </summary>
    public partial class OrderManagement : UserControl
    {
        private const int VisibleFlag = 0;
        private const int HiddenFlag = 2;
        private const int UnconfirmedFlag = 0;
        private const int ConfirmedFlag = 1;

        private MEmployee? _loggedInEmployee;
        private bool _isEditMode;
        private int? _editingOrderId;
        private OrderDisplayModel? _selectedOrder;

        // 編集中の商品明細
        private readonly ObservableCollection<OrderDetailEditModel>
            _editDetails = new();

        public OrderManagement()
        {
            InitializeComponent();

            Loaded += OrderManagement_Loaded;

            cmb_EditProduct.SelectionChanged += EditInput_Changed;
            txt_EditQuantity.TextChanged += EditInput_Changed;
        }

        public OrderManagement(MEmployee employee) : this()
        {
            _loggedInEmployee = employee
                ?? throw new ArgumentNullException(nameof(employee));
        }

        // =====================================================
        // 初期表示
        // =====================================================

        private void OrderManagement_Loaded(
            object sender, RoutedEventArgs e)
        {
            LoadMasterData();
            ExecuteSearch();
            UpdateActionButtonsState();
        }

        private void LoadMasterData()
        {
            try
            {
                using var context = new SalesManagementContext();

                var clients = context.MClients
                    .AsNoTracking()
                    .Where(c => c.ClFlag == 0)
                    .OrderBy(c => c.ClId)
                    .ToList();

                cmb_EditCustomer.ItemsSource = clients;

                var products = context.MProducts
                    .AsNoTracking()
                    .Where(p => p.PrFlag == 0)
                    .OrderBy(p => p.PrId)
                    .ToList();

                cmb_EditProduct.ItemsSource = products;

                cmb_SearchStatus.SelectedIndex = 0;
                cmb_EditOrderStatus.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"マスタ情報の読み込みに失敗しました。\n{ex.Message}",
                    "読み込みエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =====================================================
        // 注文検索
        // =====================================================

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ExecuteSearch();
        }

        private void chk_IncludeHidden_Click(
            object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void ExecuteSearch()
        {
            try
            {
                DateTime? startDate = dp_SearchStartDate.SelectedDate;
                DateTime? endDate = dp_SearchEndDate.SelectedDate;

                if (startDate.HasValue && endDate.HasValue &&
                    startDate.Value.Date > endDate.Value.Date)
                {
                    MessageBox.Show(
                        "開始日は終了日より前の日付を指定してください。",
                        "入力確認",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                using var context = new SalesManagementContext();

                var query = context.TOrders
                    .AsNoTracking()
                    .Include(o => o.Cl)
                    .Include(o => o.TOrderDetails)
                        .ThenInclude(d => d.Pr)
                    .Include(o => o.TShipments)
                    .AsQueryable();

                if (chk_IncludeHidden.IsChecked != true)
                    query = query.Where(o => o.OrFlag == VisibleFlag);

                var orders = query
                    .OrderByDescending(o => o.OrDate)
                    .ThenByDescending(o => o.OrId)
                    .ToList();

                var result = orders
                    .Select(ToDisplayModel)
                    .ToList();

                string orderId = txt_SearchOrderId.Text.Trim();

                if (!string.IsNullOrWhiteSpace(orderId))
                {
                    result = result
                        .Where(o => o.OrderId.Contains(
                            orderId,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                string customerName = txt_SearchCustomerName.Text.Trim();

                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    result = result
                        .Where(o => o.CustomerName.Contains(
                            customerName,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (startDate.HasValue)
                {
                    result = result
                        .Where(o => o.OrderDate.Date >= startDate.Value.Date)
                        .ToList();
                }

                if (endDate.HasValue)
                {
                    result = result
                        .Where(o => o.OrderDate.Date <= endDate.Value.Date)
                        .ToList();
                }

                string status = GetComboBoxText(cmb_SearchStatus);

                if (!string.IsNullOrWhiteSpace(status) &&
                    status != "すべて")
                {
                    result = result
                        .Where(o => o.OrderStatus == status)
                        .ToList();
                }

                dg_Orders.ItemsSource = result;
                txt_OrderCount.Text = $"表示件数: {result.Count:N0} 件";

                _selectedOrder = null;
                UpdateActionButtonsState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"注文情報の検索に失敗しました。\n{ex.Message}",
                    "検索エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchOrderId.Clear();
            txt_SearchCustomerName.Clear();
            cmb_SearchStatus.SelectedIndex = 0;
            dp_SearchStartDate.SelectedDate = null;
            dp_SearchEndDate.SelectedDate = null;
            chk_IncludeHidden.IsChecked = false;

            ExecuteSearch();
        }

        // =====================================================
        // 一覧表示モデル
        // =====================================================

        private OrderDisplayModel ToDisplayModel(TOrder order)
        {
            var details = order.TOrderDetails.ToList();

            int quantity = details.Sum(d => d.OrQuantity);
            decimal totalAmount = details.Sum(d => d.OrTotalPrice);

            string productName = details.Count == 0
                ? "-"
                : string.Join("、",
                    details.Select(d => d.Pr?.PrName ?? "-"));

            string productCode = details.Count == 0
                ? "-"
                : string.Join("、",
                    details.Select(d => d.Pr?.PrJcode ?? "-"));

            int statusFlag = order.OrStateFlag ?? UnconfirmedFlag;

            bool isCompleted = order.TShipments
                .Any(s => s.ShFinishDate.HasValue);

            string status = isCompleted
                ? "完了"
                : statusFlag == ConfirmedFlag
                    ? "処理中"
                    : "受付中";

            return new OrderDisplayModel
            {
                Id = order.OrId,
                OrderId = $"ORD{order.OrId:0000}",
                CustomerName = order.Cl?.ClName ?? "-",
                ProductCode = productCode,
                ProductName = productName,
                Quantity = quantity,
                OrderDate = order.OrDate,
                OrderStatus = status,
                TotalAmount = totalAmount,
                ManagementFlag = order.OrFlag,
                OrderStatusFlag = statusFlag
            };
        }

        // =====================================================
        // 一覧選択・ボタン制御
        // =====================================================

        private void dg_Orders_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            _selectedOrder =
                dg_Orders.SelectedItem as OrderDisplayModel;

            UpdateActionButtonsState();
        }

        private void UpdateActionButtonsState()
        {
            bool hasSelection = _selectedOrder != null;

            bool isVisible = hasSelection &&
                _selectedOrder!.ManagementFlag == VisibleFlag;

            bool isUnconfirmed = hasSelection &&
                _selectedOrder!.OrderStatusFlag == UnconfirmedFlag;

            btn_Register.IsEnabled = _loggedInEmployee != null;

            btn_Update.IsEnabled = isVisible &&
                isUnconfirmed &&
                _loggedInEmployee != null;

            btn_Confirm.IsEnabled = isVisible && isUnconfirmed;
            btn_Hide.IsEnabled = isVisible;
        }

        // =====================================================
        // 新規注文登録
        // =====================================================

        private void btn_Register_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckLoginEmployee())
                return;

            _isEditMode = false;
            _editingOrderId = null;

            ClearEditForm();

            txt_EditTitle.Text = "注文登録";
            txt_EditOrderId.Text = "新規登録";
            btn_SaveOrder.Content = "注文を登録";

            dp_EditOrderDate.SelectedDate = DateTime.Today;
            cmb_EditOrderStatus.SelectedIndex = 0;

            modal_OrderEdit.Visibility = Visibility.Visible;
        }

        // =====================================================
        // 注文編集
        // =====================================================

        private void btn_Update_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("編集する注文を選択してください。");
                return;
            }

            if (!CheckLoginEmployee())
                return;

            if (_selectedOrder.ManagementFlag != VisibleFlag ||
                _selectedOrder.OrderStatusFlag != UnconfirmedFlag)
            {
                MessageBox.Show(
                    "確定済み、または非表示の注文は編集できません。",
                    "編集確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new SalesManagementContext();

                var order = context.TOrders
                    .Include(o => o.TOrderDetails)
                        .ThenInclude(d => d.Pr)
                    .FirstOrDefault(o => o.OrId == _selectedOrder.Id);

                if (order == null)
                {
                    MessageBox.Show("対象の注文が見つかりません。");
                    ExecuteSearch();
                    return;
                }

                if (order.OrFlag != VisibleFlag ||
                    (order.OrStateFlag ?? UnconfirmedFlag) != UnconfirmedFlag)
                {
                    MessageBox.Show("この注文は編集できない状態です。");
                    ExecuteSearch();
                    return;
                }

                _isEditMode = true;
                _editingOrderId = order.OrId;

                ClearEditForm();

                txt_EditTitle.Text = "注文編集";
                txt_EditOrderId.Text = $"ORD{order.OrId:0000}";
                btn_SaveOrder.Content = "注文を更新";

                cmb_EditCustomer.SelectedValue = order.ClId;
                dp_EditOrderDate.SelectedDate = order.OrDate;

                // 既存の明細をすべて読み込む
                foreach (var detail in order.TOrderDetails)
                {
                    var product = detail.Pr ??
                        context.MProducts
                            .AsNoTracking()
                            .FirstOrDefault(p => p.PrId == detail.PrId);

                    if (product == null)
                        continue;

                    _editDetails.Add(new OrderDetailEditModel
                    {
                        ProductId = detail.PrId,
                        ProductName = product.PrName,
                        Quantity = detail.OrQuantity,
                        UnitPrice = product.Price,
                        TotalPrice = detail.OrTotalPrice
                    });
                }

                dg_EditDetails.ItemsSource = _editDetails;

                UpdateOrderTotal();
                ClearDetailInput();

                modal_OrderEdit.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"注文情報の取得に失敗しました。\n{ex.Message}",
                    "編集エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =====================================================
        // 商品明細の追加
        // =====================================================

        private void btn_AddDetail_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_EditProduct.SelectedItem is not MProduct product)
            {
                ShowEditError("追加する商品を選択してください。");
                return;
            }

            if (!int.TryParse(txt_EditQuantity.Text, out int quantity) ||
                quantity <= 0)
            {
                ShowEditError("商品数量は1以上の整数を入力してください。");
                return;
            }

            // 同じ商品が既にある場合は数量を合算
            var existing = _editDetails
                .FirstOrDefault(d => d.ProductId == product.PrId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.TotalPrice = existing.UnitPrice * existing.Quantity;

                RefreshDetailGrid(existing);
            }
            else
            {
                _editDetails.Add(new OrderDetailEditModel
                {
                    ProductId = product.PrId,
                    ProductName = product.PrName,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * quantity
                });
            }

            UpdateOrderTotal();
            ClearDetailInput();
            HideEditError();
        }

        // =====================================================
        // 商品明細の変更
        // =====================================================

        private void btn_UpdateDetail_Click(object sender, RoutedEventArgs e)
        {
            if (dg_EditDetails.SelectedItem is not OrderDetailEditModel selected)
            {
                ShowEditError("変更する商品明細を選択してください。");
                return;
            }

            if (cmb_EditProduct.SelectedItem is not MProduct product)
            {
                ShowEditError("商品を選択してください。");
                return;
            }

            if (!int.TryParse(txt_EditQuantity.Text, out int quantity) ||
                quantity <= 0)
            {
                ShowEditError("商品数量は1以上の整数を入力してください。");
                return;
            }

            // 同じ商品を別行に重複登録しない
            var duplicate = _editDetails.FirstOrDefault(d =>
                d != selected && d.ProductId == product.PrId);

            if (duplicate != null)
            {
                ShowEditError(
                    "同じ商品が明細に登録されています。\n" +
                    "既存の明細を選択して数量を変更してください。");
                return;
            }

            selected.ProductId = product.PrId;
            selected.ProductName = product.PrName;
            selected.Quantity = quantity;
            selected.UnitPrice = product.Price;
            selected.TotalPrice = product.Price * quantity;

            RefreshDetailGrid(selected);
            UpdateOrderTotal();
            ClearDetailInput();
            HideEditError();
        }

        // =====================================================
        // 商品明細の削除
        // =====================================================

        private void btn_RemoveDetail_Click(object sender, RoutedEventArgs e)
        {
            if (dg_EditDetails.SelectedItem is not OrderDetailEditModel selected)
            {
                ShowEditError("削除する商品明細を選択してください。");
                return;
            }

            var result = MessageBox.Show(
                $"商品「{selected.ProductName}」を明細から削除しますか？",
                "明細削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _editDetails.Remove(selected);

            UpdateOrderTotal();
            ClearDetailInput();
            HideEditError();
        }

        private void dg_EditDetails_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dg_EditDetails.SelectedItem is not OrderDetailEditModel detail)
                return;

            cmb_EditProduct.SelectedValue = detail.ProductId;
            txt_EditQuantity.Text = detail.Quantity.ToString();

            HideEditError();
        }

        private void RefreshDetailGrid(OrderDetailEditModel item)
        {
            int index = _editDetails.IndexOf(item);

            if (index >= 0)
            {
                _editDetails[index] = new OrderDetailEditModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                };

                dg_EditDetails.SelectedItem = _editDetails[index];
            }

            dg_EditDetails.Items.Refresh();
        }

        // =====================================================
        // 注文保存
        // =====================================================

        private void btn_SaveOrder_Click(object sender, RoutedEventArgs e)
        {
            HideEditError();

            if (!CheckLoginEmployee())
            {
                ShowEditError("ログイン社員情報を取得できません。");
                return;
            }

            if (cmb_EditCustomer.SelectedValue == null)
            {
                ShowEditError("顧客を選択してください。");
                return;
            }

            if (_editDetails.Count == 0)
            {
                ShowEditError("商品明細を1件以上追加してください。");
                return;
            }

            if (!dp_EditOrderDate.SelectedDate.HasValue)
            {
                ShowEditError("注文日を選択してください。");
                return;
            }

            int clientId = Convert.ToInt32(
                cmb_EditCustomer.SelectedValue);

            DateTime orderDate =
                dp_EditOrderDate.SelectedDate.Value.Date;

            try
            {
                using var context = new SalesManagementContext();

                using var transaction =
                    context.Database.BeginTransaction(
                        IsolationLevel.Serializable);

                var client = context.MClients
                    .FirstOrDefault(c =>
                        c.ClId == clientId &&
                        c.ClFlag == VisibleFlag);

                if (client == null)
                {
                    ShowEditError(
                        "選択した顧客が存在しないか、非表示になっています。");
                    return;
                }

                // DB上の商品を再確認
                var productIds = _editDetails
                    .Select(d => d.ProductId)
                    .Distinct()
                    .ToList();

                var products = context.MProducts
                    .Where(p =>
                        productIds.Contains(p.PrId) &&
                        p.PrFlag == VisibleFlag)
                    .ToDictionary(p => p.PrId);

                if (products.Count != productIds.Count)
                {
                    ShowEditError(
                        "明細に存在しない商品、または非表示の商品が含まれています。");
                    return;
                }

                // 商品マスタの最新単価で金額を再計算
                foreach (var detail in _editDetails)
                {
                    if (detail.Quantity <= 0)
                    {
                        ShowEditError("商品数量は1以上にしてください。");
                        return;
                    }

                    detail.UnitPrice = products[detail.ProductId].Price;
                    detail.ProductName = products[detail.ProductId].PrName;
                    detail.TotalPrice =
                        detail.UnitPrice * detail.Quantity;
                }

                if (_isEditMode)
                {
                    if (!_editingOrderId.HasValue)
                    {
                        ShowEditError("編集対象の注文番号がありません。");
                        return;
                    }

                    var order = context.TOrders
                        .Include(o => o.TOrderDetails)
                        .FirstOrDefault(o =>
                            o.OrId == _editingOrderId.Value);

                    if (order == null)
                    {
                        ShowEditError("編集対象の注文が見つかりません。");
                        return;
                    }

                    if (order.OrFlag != VisibleFlag ||
                        (order.OrStateFlag ?? UnconfirmedFlag)
                            != UnconfirmedFlag)
                    {
                        ShowEditError("この注文は編集できない状態です。");
                        return;
                    }

                    order.ClId = clientId;
                    order.OrDate = orderDate;

                    // 既存明細をすべて削除対象にする
                    var oldDetails = order.TOrderDetails.ToList();

                    context.TOrderDetails.RemoveRange(oldDetails);

                    order.TOrderDetails.Clear();

                    // 編集画面の明細をすべて再登録
                    foreach (var detail in _editDetails)
                    {
                        order.TOrderDetails.Add(new TOrderDetail
                        {
                            PrId = detail.ProductId,
                            OrQuantity = detail.Quantity,
                            OrTotalPrice = detail.TotalPrice
                        });
                    }

                    context.SaveChanges();
                    transaction.Commit();

                    MessageBox.Show(
                        "注文情報を更新しました。",
                        "更新完了",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    var newOrder = new TOrder
                    {
                        SoId = _loggedInEmployee.SoId,
                        EmId = _loggedInEmployee.EmId,
                        ClId = clientId,
                        ClCharge = null,
                        OrDate = orderDate,
                        OrStateFlag = UnconfirmedFlag,
                        OrFlag = VisibleFlag,
                        OrHidden = null
                    };

                    foreach (var detail in _editDetails)
                    {
                        newOrder.TOrderDetails.Add(new TOrderDetail
                        {
                            PrId = detail.ProductId,
                            OrQuantity = detail.Quantity,
                            OrTotalPrice = detail.TotalPrice
                        });
                    }

                    context.TOrders.Add(newOrder);

                    context.SaveChanges();
                    transaction.Commit();

                    MessageBox.Show(
                        $"注文を登録しました。\n注文番号：ORD{newOrder.OrId:0000}",
                        "登録完了",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                modal_OrderEdit.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            catch (Exception ex)
            {
                ShowEditError(
                    $"注文保存中にエラーが発生しました。\n{ex.Message}");
            }
        }

        // =====================================================
        // 注文非表示
        // =====================================================

        private void btn_Hide_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("非表示にする注文を選択してください。");
                return;
            }

            if (_selectedOrder.ManagementFlag != VisibleFlag)
            {
                MessageBox.Show("この注文は既に非表示です。");
                return;
            }

            var result = MessageBox.Show(
                $"注文番号「{_selectedOrder.OrderId}」を非表示にしますか？",
                "非表示確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var context = new SalesManagementContext();

                var order = context.TOrders
                    .FirstOrDefault(o => o.OrId == _selectedOrder.Id);

                if (order == null)
                {
                    MessageBox.Show("対象の注文が見つかりません。");
                    ExecuteSearch();
                    return;
                }

                if (order.OrFlag != VisibleFlag)
                {
                    MessageBox.Show("この注文は既に非表示です。");
                    ExecuteSearch();
                    return;
                }

                order.OrFlag = HiddenFlag;
                order.OrHidden = "注文管理画面から非表示設定";

                context.SaveChanges();

                MessageBox.Show(
                    "注文を非表示にしました。",
                    "処理完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ExecuteSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"注文の非表示設定に失敗しました。\n{ex.Message}",
                    "処理エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =====================================================
        // 注文確定
        // =====================================================

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("確定する注文を選択してください。");
                return;
            }

            var selected = _selectedOrder;

            if (selected.ManagementFlag != VisibleFlag)
            {
                MessageBox.Show("非表示の注文は確定できません。");
                return;
            }

            if (selected.OrderStatusFlag != UnconfirmedFlag)
            {
                MessageBox.Show("この注文は既に確定されています。");
                return;
            }

            var result = MessageBox.Show(
                $"注文番号「{selected.OrderId}」を確定します。\n\n" +
                "在庫を減算し、出荷管理にデータを登録します。\n" +
                "よろしいですか？",
                "注文確定確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                ConfirmOrder(selected.Id);
        }

        private void ConfirmOrder(int orderId)
        {
            try
            {
                using var context = new SalesManagementContext();

                using var transaction =
                    context.Database.BeginTransaction(
                        IsolationLevel.Serializable);

                var order = context.TOrders
                    .Include(o => o.TOrderDetails)
                    .FirstOrDefault(o => o.OrId == orderId);

                if (order == null)
                {
                    MessageBox.Show("対象の注文が見つかりません。");
                    return;
                }

                if (order.OrFlag != VisibleFlag)
                {
                    MessageBox.Show("非表示の注文は確定できません。");
                    return;
                }

                if ((order.OrStateFlag ?? 0) != UnconfirmedFlag)
                {
                    MessageBox.Show("この注文は既に確定されています。");
                    return;
                }

                if (order.TOrderDetails.Count == 0)
                {
                    MessageBox.Show("注文商品が登録されていません。");
                    return;
                }

                if (context.TShipments.Any(s => s.OrId == order.OrId))
                {
                    MessageBox.Show(
                        "この注文の出荷データは既に登録されています。",
                        "重複確認",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 明細の商品ごとに在庫を確認・減算
                foreach (var detail in order.TOrderDetails)
                {
                    if (detail.OrQuantity <= 0)
                    {
                        MessageBox.Show("注文数量が正しくありません。");
                        return;
                    }

                    var stockRows = context.TStocks
                        .Where(s =>
                            s.PrId == detail.PrId &&
                            s.StFlag == VisibleFlag)
                        .OrderBy(s => s.StId)
                        .ToList();

                    if (stockRows.Count == 0)
                    {
                        MessageBox.Show(
                            $"商品ID {detail.PrId} の有効な在庫がありません。");
                        return;
                    }

                    int totalStock = stockRows.Sum(s => s.StQuantity);

                    if (totalStock < detail.OrQuantity)
                    {
                        string productName = context.MProducts
                            .Where(p => p.PrId == detail.PrId)
                            .Select(p => p.PrName)
                            .FirstOrDefault() ?? $"商品ID {detail.PrId}";

                        MessageBox.Show(
                            $"商品「{productName}」の在庫が不足しています。\n" +
                            $"現在の在庫数：{totalStock:N0} 個\n" +
                            $"注文数量：{detail.OrQuantity:N0} 個",
                            "在庫不足",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    int remaining = detail.OrQuantity;

                    foreach (var stock in stockRows)
                    {
                        if (remaining <= 0)
                            break;

                        int decrease = Math.Min(
                            stock.StQuantity,
                            remaining);

                        stock.StQuantity -= decrease;
                        remaining -= decrease;
                    }

                    if (remaining > 0)
                    {
                        MessageBox.Show("在庫の減算に失敗しました。");
                        return;
                    }
                }

                // 出荷ヘッダー
                var shipment = new TShipment
                {
                    ClId = order.ClId,
                    EmId = _loggedInEmployee?.EmId ?? order.EmId,
                    SoId = order.SoId,
                    OrId = order.OrId,
                    ShStateFlag = null,
                    ShFinishDate = null,
                    ShFlag = VisibleFlag,
                    ShHidden = null
                };

                // 出荷明細を全商品分作成
                foreach (var detail in order.TOrderDetails)
                {
                    shipment.TShipmentDetails.Add(
                        new TShipmentDetail
                        {
                            PrId = detail.PrId,
                            ShQuantity = detail.OrQuantity
                        });
                }

                context.TShipments.Add(shipment);

                order.OrStateFlag = ConfirmedFlag;

                context.SaveChanges();
                transaction.Commit();

                MessageBox.Show(
                    $"注文番号「ORD{order.OrId:0000}」を確定しました。\n\n" +
                    "・全商品の在庫を減算しました。\n" +
                    "・出荷管理に明細を登録しました。\n" +
                    "・注文状態を確定済みに変更しました。",
                    "注文確定完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ExecuteSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"注文確定処理に失敗しました。\n{ex.Message}",
                    "確定エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                ExecuteSearch();
            }
        }

        // =====================================================
        // ダブルクリック編集
        // =====================================================

        private void dg_Orders_MouseDoubleClick(
            object sender, MouseButtonEventArgs e)
        {
            if (_selectedOrder == null)
                return;

            if (_selectedOrder.ManagementFlag != VisibleFlag ||
                _selectedOrder.OrderStatusFlag != UnconfirmedFlag)
                return;

            btn_Update_Click(sender, new RoutedEventArgs());
        }

        // =====================================================
        // 明細入力欄
        // =====================================================

        private void EditInput_Changed(object sender, EventArgs e)
        {
            UpdateDetailInputTotal();
        }

        private void UpdateDetailInputTotal()
        {
            if (txt_DetailAmount == null)
                return;

            if (cmb_EditProduct.SelectedItem is not MProduct product ||
                !int.TryParse(txt_EditQuantity.Text, out int quantity) ||
                quantity <= 0)
            {
                txt_DetailAmount.Text = "0 円";
                return;
            }

            txt_DetailAmount.Text =
                $"{product.Price * quantity:N0} 円";
        }

        private void UpdateOrderTotal()
        {
            decimal total = _editDetails.Sum(d => d.TotalPrice);

            txt_EditTotalAmount.Text = $"{total:N0} 円";
        }

        private void ClearDetailInput()
        {
            dg_EditDetails.SelectedItem = null;
            cmb_EditProduct.SelectedIndex = -1;
            txt_EditQuantity.Text = "1";
            txt_DetailAmount.Text = "0 円";
        }

        // =====================================================
        // フォーム初期化
        // =====================================================

        private void ClearEditForm()
        {
            txt_EditOrderId.Text = string.Empty;
            cmb_EditCustomer.SelectedIndex = -1;
            cmb_EditProduct.SelectedIndex = -1;
            txt_EditQuantity.Text = "1";
            dp_EditOrderDate.SelectedDate = DateTime.Today;
            cmb_EditOrderStatus.SelectedIndex = 0;

            _editDetails.Clear();

            dg_EditDetails.ItemsSource = _editDetails;

            txt_EditTotalAmount.Text = "0 円";
            txt_DetailAmount.Text = "0 円";

            HideEditError();
        }

        // =====================================================
        // モーダル操作
        // =====================================================

        private void btn_CloseEdit_Click(
            object sender, RoutedEventArgs e)
        {
            modal_OrderEdit.Visibility = Visibility.Collapsed;
        }

        private void modal_OrderEdit_Backdrop_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
                modal_OrderEdit.Visibility = Visibility.Collapsed;
        }

        // =====================================================
        // エラー表示
        // =====================================================

        private void ShowEditError(string message)
        {
            border_EditError.Visibility = Visibility.Visible;
            txt_EditError.Text = message;
        }

        private void HideEditError()
        {
            border_EditError.Visibility = Visibility.Collapsed;
            txt_EditError.Text = string.Empty;
        }

        // =====================================================
        // ログイン社員確認
        // =====================================================

        private bool CheckLoginEmployee()
        {
            if (_loggedInEmployee != null)
                return true;

            MessageBox.Show(
                "ログイン中の社員情報が設定されていません。\n" +
                "画面を開くときにログイン社員を渡してください。",
                "ログイン情報エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        private string GetComboBoxText(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? string.Empty;

            return comboBox.Text?.Trim() ?? string.Empty;
        }
    }

    // =============================================================
    // 注文一覧表示モデル
    // =============================================================

    public class OrderDisplayModel
    {
        public int Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ManagementFlag { get; set; }
        public int OrderStatusFlag { get; set; }

        public string FlagFormatted =>
            ManagementFlag switch
            {
                0 => "表示中",
                2 => "非表示",
                _ => $"その他({ManagementFlag})"
            };
    }

    // =============================================================
    // 注文編集用の商品明細モデル
    // =============================================================

    public class OrderDetailEditModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
