

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 機能12 出庫管理。
    /// 出庫登録・更新時に在庫数を減算し、更新時は旧出庫分を戻してから再計算する。
    /// </summary>
    public partial class Shipping : UserControl
    {
        private const int ActiveFlag = 0;
        private const int HiddenFlag = 2;

        private bool _isEditing;
        private bool _loadingCombo;
        private int? _editingSyId;
        private int? _editingSyDetailId;

        public Shipping()
        {
            InitializeComponent();
            chk_IncludeHidden.Checked += chk_IncludeHidden_Checked;
            chk_IncludeHidden.Unchecked += chk_IncludeHidden_Unchecked;
            LoadShippings();
        }

        private void LoadShippings()
        {
            try
            {
                using var context = new SalesManagementContext();

                var query = context.TSyukkos
                    .AsNoTracking()
                    .Include(s => s.Or).ThenInclude(o => o.Cl)
                    .Include(s => s.Cl)
                    .Include(s => s.Em)
                    .Include(s => s.TSyukkoDetails).ThenInclude(d => d.Pr)
                    .AsQueryable();

                if (chk_IncludeHidden.IsChecked != true)
                    query = query.Where(s => s.SyFlag == ActiveFlag);

                var rows = query
                    .OrderByDescending(s => s.SyDate)
                    .ThenByDescending(s => s.SyId)
                    .ToList()
                    .SelectMany(s => s.TSyukkoDetails.Select(d => new ShippingRow
                    {
                        SyId = s.SyId,
                        OrId = s.OrId,
                        ClId = s.ClId,
                        SoId = s.SoId,
                        EmId = s.EmId,
                        SyDate = s.SyDate,
                        SyFlag = s.SyFlag,
                        SyHidden = s.SyHidden ?? string.Empty,
                        SyStateFlag = s.SyStateFlag,
                        SyDetailId = d.SyDetailId,
                        PrId = d.PrId,
                        ProductName = d.Pr?.PrName ?? "（商品名なし）",
                        SyQuantity = d.SyQuantity,
                        EmployeeName = s.Em?.EmName ?? "未設定",
                        ClientName = s.Cl?.ClName ?? s.Or?.Cl?.ClName ?? "未設定",
                        FlagDisplay = s.SyFlag == HiddenFlag
                            ? "非表示 (2)"
                            : $"通常 ({s.SyFlag})"
                    }))
                    .ToList();

                dg_Syukkos.ItemsSource = rows;
                txt_RecordCount.Text = $"表示件数: {rows.Count} 件";
            }
            catch (Exception ex)
            {
                ShowError("出庫一覧の取得に失敗しました。\n" + ex.Message);
            }
        }

        /// <summary>
        /// 出庫IDの入力内容を受注ID欄に自動コピーする（既存UIの動作を維持）
        /// </summary>
        private void chk_IncludeHidden_Checked(
            object sender, RoutedEventArgs e)
        {
            LoadShippings();
        }

        private void chk_IncludeHidden_Unchecked(
            object sender, RoutedEventArgs e)
        {
            LoadShippings();
        }

        private void txt_SearchSyId_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (txt_SearchOrId.Text != txt_SearchSyId.Text)
            {
                txt_SearchOrId.Text = txt_SearchSyId.Text;
                txt_SearchOrId.CaretIndex = txt_SearchOrId.Text.Length;
            }
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseOptionalInt(txt_SearchSyId.Text, out int? syId) ||
                !TryParseOptionalInt(txt_SearchOrId.Text, out int? orId))
            {
                ShowWarning("出庫ID・受注IDは1以上の整数で入力してください。");
                return;
            }

            try
            {
                using var context = new SalesManagementContext();

                var query = context.TSyukkos
                    .AsNoTracking()
                    .Include(s => s.Or).ThenInclude(o => o.Cl)
                    .Include(s => s.Cl)
                    .Include(s => s.Em)
                    .Include(s => s.TSyukkoDetails).ThenInclude(d => d.Pr)
                    .AsQueryable();

                if (syId.HasValue)
                    query = query.Where(s => s.SyId == syId.Value);
                if (orId.HasValue)
                    query = query.Where(s => s.OrId == orId.Value);
                if (chk_IncludeHidden.IsChecked != true)
                    query = query.Where(s => s.SyFlag == ActiveFlag);

                string productName = txt_SearchProductName.Text?.Trim() ?? string.Empty;

                var rows = query
                    .OrderByDescending(s => s.SyDate)
                    .ThenByDescending(s => s.SyId)
                    .ToList()
                    .SelectMany(s => s.TSyukkoDetails
                        .Where(d => string.IsNullOrWhiteSpace(productName) ||
                            (d.Pr?.PrName?.Contains(
                                productName, StringComparison.OrdinalIgnoreCase) ?? false))
                        .Select(d => new ShippingRow
                        {
                            SyId = s.SyId,
                            OrId = s.OrId,
                            ClId = s.ClId,
                            SoId = s.SoId,
                            EmId = s.EmId,
                            SyDate = s.SyDate,
                            SyFlag = s.SyFlag,
                            SyHidden = s.SyHidden ?? string.Empty,
                            SyStateFlag = s.SyStateFlag,
                            SyDetailId = d.SyDetailId,
                            PrId = d.PrId,
                            ProductName = d.Pr?.PrName ?? "（商品名なし）",
                            SyQuantity = d.SyQuantity,
                            EmployeeName = s.Em?.EmName ?? "未設定",
                            ClientName = s.Cl?.ClName ?? s.Or?.Cl?.ClName ?? "未設定",
                            FlagDisplay = s.SyFlag == HiddenFlag
                                ? "非表示 (2)"
                                : $"通常 ({s.SyFlag})"
                        }))
                    .ToList();

                dg_Syukkos.ItemsSource = rows;
                txt_RecordCount.Text = $"表示件数: {rows.Count} 件";
            }
            catch (Exception ex)
            {
                ShowError("出庫検索に失敗しました。\n" + ex.Message);
            }
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchSyId.Clear();
            txt_SearchOrId.Clear();
            txt_SearchProductName.Clear();
            chk_IncludeHidden.IsChecked = false;
            LoadShippings();
        }

        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            _editingSyId = null;
            _editingSyDetailId = null;

            txt_EditorTitle.Text = "新規出庫登録";
            btn_ExecuteEditor.Content = "登録実行";
            btn_ExecuteEditor.Background = System.Windows.Media.Brushes.Teal;

            LoadEditorChoices();

            dp_SyDate.SelectedDate = DateTime.Today;
            txt_SyQuantity.Text = "1";
            cmb_OrId.SelectedIndex = -1;
            cmb_Product.ItemsSource = null;
            cmb_Employee.SelectedIndex = -1;

            modal_Editor.Visibility = Visibility.Visible;
        }

        private void btn_EditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Syukkos.SelectedItem is not ShippingRow selected)
            {
                ShowWarning("編集する出庫情報を一覧から選択してください。");
                return;
            }

            _isEditing = true;
            _editingSyId = selected.SyId;
            _editingSyDetailId = selected.SyDetailId;

            txt_EditorTitle.Text = $"出庫情報編集（出庫ID: {selected.SyId}）";
            btn_ExecuteEditor.Content = "更新保存";
            btn_ExecuteEditor.Background = System.Windows.Media.Brushes.SteelBlue;

            LoadEditorChoices();
            cmb_OrId.SelectedValue = selected.OrId;
            LoadProductsForOrder(selected.OrId);
            cmb_Product.SelectedValue = selected.PrId;
            cmb_Employee.SelectedValue = selected.EmId;
            dp_SyDate.SelectedDate = selected.SyDate?.Date ?? DateTime.Today;
            txt_SyQuantity.Text = selected.SyQuantity.ToString();

            modal_Editor.Visibility = Visibility.Visible;
        }

        private void LoadEditorChoices()
        {
            try
            {
                using var context = new SalesManagementContext();
                _loadingCombo = true;

                cmb_OrId.ItemsSource = context.TOrders
                    .AsNoTracking()
                    .Include(o => o.Cl)
                    .Where(o => o.OrFlag == ActiveFlag)
                    .OrderByDescending(o => o.OrDate)
                    .Select(o => new OrderChoice
                    {
                        OrId = o.OrId,
                        OrDate = o.OrDate,
                        ClId = o.ClId,
                        SoId = o.SoId,
                        ClientName = o.Cl.ClName
                    })
                    .ToList();

                cmb_Employee.ItemsSource = context.MEmployees
                    .AsNoTracking()
                    .Where(em => em.EmFlag == ActiveFlag)
                    .OrderBy(em => em.EmId)
                    .ToList();

                _loadingCombo = false;
            }
            catch (Exception ex)
            {
                _loadingCombo = false;
                ShowError("受注・社員の選択肢を取得できませんでした。\n" + ex.Message);
            }
        }

        private void cmb_OrId_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_loadingCombo || cmb_OrId.SelectedValue == null ||
                !int.TryParse(cmb_OrId.SelectedValue.ToString(), out int orId))
                return;

            LoadProductsForOrder(orId);
        }

        private void LoadProductsForOrder(int orId)
        {
            try
            {
                using var context = new SalesManagementContext();

                var products = context.TOrderDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.OrId == orId)
                    .OrderBy(d => d.PrId)
                    .Select(d => new ProductChoice
                    {
                        PrId = d.PrId,
                        PrName = d.Pr.PrName,
                        OrderedQuantity = d.OrQuantity
                    })
                    .ToList();

                cmb_Product.ItemsSource = products;
                if (products.Count == 1 && !_isEditing)
                    cmb_Product.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError("受注明細の商品を取得できませんでした。\n" + ex.Message);
            }
        }

        /// <summary>
        /// 出庫登録・更新。保存と在庫減算を同一トランザクションで行う。
        /// </summary>
        private void btn_ExecuteEditor_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_OrId.SelectedValue == null ||
                !int.TryParse(cmb_OrId.SelectedValue.ToString(), out int orId))
            {
                ShowWarning("受注を選択してください。");
                return;
            }

            if (dp_SyDate.SelectedDate == null)
            {
                ShowWarning("出庫日を指定してください。");
                return;
            }

            if (cmb_Product.SelectedValue == null ||
                !int.TryParse(cmb_Product.SelectedValue.ToString(), out int prId))
            {
                ShowWarning("選択した受注に含まれる商品を選択してください。");
                return;
            }

            if (!int.TryParse(txt_SyQuantity.Text, out int quantity) || quantity <= 0)
            {
                ShowWarning("出庫数量には1以上の整数を入力してください。");
                return;
            }

            int? emId = null;
            if (cmb_Employee.SelectedValue != null &&
                int.TryParse(cmb_Employee.SelectedValue.ToString(), out int selectedEmId))
                emId = selectedEmId;

            try
            {
                using var context = new SalesManagementContext();
                using var transaction = context.Database.BeginTransaction();

                var order = context.TOrders.FirstOrDefault(o => o.OrId == orId);
                if (order == null)
                {
                    ShowWarning("選択した受注情報が見つかりません。");
                    return;
                }

                var orderDetail = context.TOrderDetails
                    .FirstOrDefault(d => d.OrId == orId && d.PrId == prId);
                if (orderDetail == null)
                {
                    ShowWarning("選択した商品は受注明細に存在しません。");
                    return;
                }

                int? oldSyId = _isEditing ? _editingSyId : null;
                int alreadyShipped = context.TSyukkoDetails
                    .Include(d => d.Sy)
                    .Where(d => d.Sy.OrId == orId && d.PrId == prId &&
                                (!oldSyId.HasValue || d.SyId != oldSyId.Value))
                    .Sum(d => (int?)d.SyQuantity) ?? 0;

                int remainingOrderQuantity = orderDetail.OrQuantity - alreadyShipped;
                if (quantity > remainingOrderQuantity)
                {
                    ShowWarning($"受注数量を超えています。今回出庫できる残り数量は {Math.Max(0, remainingOrderQuantity)} 個です。");
                    return;
                }

                if (_isEditing)
                {
                    if (!_editingSyId.HasValue || !_editingSyDetailId.HasValue)
                    {
                        ShowWarning("更新対象を特定できません。もう一度選択してください。");
                        return;
                    }

                    var shipping = context.TSyukkos
                        .FirstOrDefault(s => s.SyId == _editingSyId.Value);
                    var detail = context.TSyukkoDetails
                        .FirstOrDefault(d => d.SyDetailId == _editingSyDetailId.Value);

                    if (shipping == null || detail == null)
                    {
                        ShowWarning("更新対象の出庫情報が見つかりません。");
                        return;
                    }

                    // 旧出庫分を在庫へ戻してから、新しい出庫分を差し引く。
                    ChangeStock(context, detail.PrId, detail.SyQuantity);

                    shipping.OrId = order.OrId;
                    shipping.ClId = order.ClId;
                    shipping.SoId = order.SoId;
                    shipping.EmId = emId;
                    shipping.SyDate = dp_SyDate.SelectedDate.Value.Date;
                    shipping.SyFlag = ActiveFlag;

                    detail.SyId = shipping.SyId;
                    detail.PrId = prId;
                    detail.SyQuantity = quantity;

                    ChangeStock(context, prId, -quantity);
                    context.SaveChanges();
                    transaction.Commit();
                    ShowInfo("出庫情報を更新し、在庫数に反映しました。");
                }
                else
                {
                    var shipping = new TSyukko
                    {
                        OrId = order.OrId,
                        ClId = order.ClId,
                        SoId = order.SoId,
                        EmId = emId,
                        SyDate = dp_SyDate.SelectedDate.Value.Date,
                        SyStateFlag = 0,
                        SyHidden = null,
                        SyFlag = ActiveFlag
                    };

                    shipping.TSyukkoDetails.Add(new TSyukkoDetail
                    {
                        PrId = prId,
                        SyQuantity = quantity
                    });

                    context.TSyukkos.Add(shipping);
                    ChangeStock(context, prId, -quantity);
                    context.SaveChanges();
                    transaction.Commit();
                    ShowInfo("出庫情報を登録し、在庫数を減算しました。");
                }

                modal_Editor.Visibility = Visibility.Collapsed;
                LoadShippings();
            }
            catch (Exception ex)
            {
                ShowError("出庫情報の保存に失敗しました。\n" + ex.Message);
            }
        }

        /// <summary>
        /// 在庫数量を増減する。正数で加算、負数で減算する。
        /// </summary>
        private static void ChangeStock(
            SalesManagementContext context, int prId, int delta)
        {
            var stock = context.TStocks
                .FirstOrDefault(s => s.PrId == prId && s.StFlag == ActiveFlag);

            if (stock == null)
                throw new InvalidOperationException($"商品ID {prId} の有効な在庫レコードがありません。");

            if (stock.StQuantity + delta < 0)
                throw new InvalidOperationException(
                    $"商品ID {prId} の在庫が不足しています。現在庫: {stock.StQuantity} 個");

            stock.StQuantity += delta;
        }

        private void btn_HideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Syukkos.SelectedItem is not ShippingRow selected)
            {
                ShowWarning("非表示にする出庫情報を一覧から選択してください。");
                return;
            }

            hide_txt_TargetInfo.Text =
                $"出庫ID: {selected.SyId} / 受注ID: {selected.OrId} / 商品: {selected.ProductName}";
            hide_txt_Reason.Text = selected.SyHidden;
            modal_Hide.Tag = selected.SyId;
            modal_Hide.Visibility = Visibility.Visible;
        }

        private void btn_ExecuteHide_Click(object sender, RoutedEventArgs e)
        {
            if (modal_Hide.Tag is not int syId)
            {
                ShowWarning("非表示対象を特定できません。");
                return;
            }

            try
            {
                using var context = new SalesManagementContext();
                var shipping = context.TSyukkos.FirstOrDefault(s => s.SyId == syId);

                if (shipping == null)
                {
                    ShowWarning("対象の出庫情報が見つかりません。");
                    return;
                }

                shipping.SyFlag = HiddenFlag;
                if (!string.IsNullOrWhiteSpace(hide_txt_Reason.Text))
                    shipping.SyHidden = hide_txt_Reason.Text.Trim();

                context.SaveChanges();
                modal_Hide.Visibility = Visibility.Collapsed;
                ShowInfo("出庫情報を非表示にしました。");
                LoadShippings();
            }
            catch (Exception ex)
            {
                ShowError("出庫情報の非表示に失敗しました。\n" + ex.Message);
            }
        }

        private void btn_CancelEditor_Click(object sender, RoutedEventArgs e)
        {
            modal_Editor.Visibility = Visibility.Collapsed;
        }

        private void modal_Editor_Backdrop_Click(
            object sender, MouseButtonEventArgs e)
        {
            modal_Editor.Visibility = Visibility.Collapsed;
        }

        private void btn_CancelHide_Click(object sender, RoutedEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        private void modal_Hide_Backdrop_Click(
            object sender, MouseButtonEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        private static bool TryParseOptionalInt(string? text, out int? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text))
                return true;
            if (!int.TryParse(text.Trim(), out int parsed) || parsed < 1)
                return false;
            value = parsed;
            return true;
        }

        private static void ShowWarning(string message) =>
            MessageBox.Show(message, "出庫管理", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowError(string message) =>
            MessageBox.Show(message, "出庫管理", MessageBoxButton.OK, MessageBoxImage.Error);

        private static void ShowInfo(string message) =>
            MessageBox.Show(message, "出庫管理", MessageBoxButton.OK, MessageBoxImage.Information);

        private sealed class ShippingRow
        {
            public int SyId { get; set; }
            public int OrId { get; set; }
            public int ClId { get; set; }
            public int SoId { get; set; }
            public int? EmId { get; set; }
            public DateTime? SyDate { get; set; }
            public int SyFlag { get; set; }
            public string SyHidden { get; set; } = string.Empty;
            public int? SyStateFlag { get; set; }
            public int SyDetailId { get; set; }
            public int PrId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int SyQuantity { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string ClientName { get; set; } = string.Empty;
            public string FlagDisplay { get; set; } = string.Empty;
        }

        private sealed class OrderChoice
        {
            public int OrId { get; set; }
            public DateTime OrDate { get; set; }
            public int ClId { get; set; }
            public int SoId { get; set; }
            public string ClientName { get; set; } = string.Empty;
            public string DisplayText => $"受注ID: {OrId} / {OrDate:yyyy/MM/dd} / {ClientName}";
        }

        private sealed class ProductChoice
        {
            public int PrId { get; set; }
            public string PrName { get; set; } = string.Empty;
            public int OrderedQuantity { get; set; }
            public string DisplayText => $"{PrId} / {PrName}（受注数量: {OrderedQuantity}）";
        }
    }
}
