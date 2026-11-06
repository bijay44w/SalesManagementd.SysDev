using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 機能11 入庫管理。
    /// 入庫登録時に棚入れ済みとして確定し、在庫数へ反映する。
    /// </summary>
    public partial class Receiving : UserControl
    {
        private const int ActiveFlag = 0;
        private const int HiddenFlag = 2;
        private const int ShelvedFlag = 1;

        private bool _isEditing;
        private bool _loadingCombo;
        private int? _editingWaId;
        private int? _editingWaDetailId;

        public Receiving()
        {
            InitializeComponent();
            chk_IncludeHidden.Unchecked += chk_IncludeHidden_Unchecked;
            LoadWarehousings();
        }

        /// <summary>
        /// 入庫一覧を読み込む
        /// </summary>
        private void LoadWarehousings()
        {
            try
            {
                using var context = new SalesManagementContext();

                var query = context.TWarehousings
                    .AsNoTracking()
                    .Include(w => w.Ha)
                    .Include(w => w.Em)
                    .Include(w => w.TWarehousingDetails)
                        .ThenInclude(d => d.Pr)
                    .AsQueryable();

                if (chk_IncludeHidden.IsChecked != true)
                    query = query.Where(w => w.WaFlag == ActiveFlag);

                var rows = query
                    .OrderByDescending(w => w.WaDate)
                    .ThenByDescending(w => w.WaId)
                    .ToList()
                    .SelectMany(w => w.TWarehousingDetails.Select(d =>
                        new ReceivingRow
                        {
                            WaId = w.WaId,
                            HaId = w.HaId,
                            EmId = w.EmId,
                            WaDate = w.WaDate,
                            WaShelfFlag = w.WaShelfFlag,
                            WaFlag = w.WaFlag,
                            WaHidden = w.WaHidden ?? string.Empty,
                            WaDetailId = d.WaDetailId,
                            PrId = d.PrId,
                            ProductName = d.Pr?.PrName ?? "（商品名なし）",
                            WaQuantity = d.WaQuantity,
                            EmployeeName = w.Em?.EmName ?? "未設定",
                            FlagDisplay = w.WaFlag == HiddenFlag
                                ? "非表示 (2)"
                                : w.WaShelfFlag == ShelvedFlag
                                    ? "棚入れ済み"
                                    : "未棚入れ"
                        }))
                    .ToList();

                dg_Warehousings.ItemsSource = rows;
                txt_RecordCount.Text = $"表示件数: {rows.Count} 件";
            }
            catch (Exception ex)
            {
                ShowError("入庫一覧の取得に失敗しました。\n" + ex.Message);
            }
        }

        /// <summary>
        /// 入庫IDを発注ID欄へ自動コピーする（既存UIの動作を維持）
        /// </summary>
        private void txt_SearchWaId_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (txt_SearchHaId.Text != txt_SearchWaId.Text)
            {
                txt_SearchHaId.Text = txt_SearchWaId.Text;
                txt_SearchHaId.CaretIndex = txt_SearchHaId.Text.Length;
            }
        }

        private void chk_IncludeHidden_Checked(
            object sender, RoutedEventArgs e)
        {
            LoadWarehousings();
        }

        private void chk_IncludeHidden_Unchecked(
            object sender, RoutedEventArgs e)
        {
            LoadWarehousings();
        }

        /// <summary>
        /// 入庫情報を検索する
        /// </summary>
        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseOptionalInt(txt_SearchWaId.Text, out int? waId) ||
                !TryParseOptionalInt(txt_SearchHaId.Text, out int? haId))
            {
                ShowWarning("入庫ID・発注IDは1以上の整数で入力してください。");
                return;
            }

            try
            {
                using var context = new SalesManagementContext();

                var query = context.TWarehousings
                    .AsNoTracking()
                    .Include(w => w.Ha)
                    .Include(w => w.Em)
                    .Include(w => w.TWarehousingDetails)
                        .ThenInclude(d => d.Pr)
                    .AsQueryable();

                if (waId.HasValue)
                    query = query.Where(w => w.WaId == waId.Value);
                if (haId.HasValue)
                    query = query.Where(w => w.HaId == haId.Value);
                if (chk_IncludeHidden.IsChecked != true)
                    query = query.Where(w => w.WaFlag == ActiveFlag);

                string productName = txt_SearchProductName.Text?.Trim() ?? string.Empty;

                var rows = query
                    .OrderByDescending(w => w.WaDate)
                    .ThenByDescending(w => w.WaId)
                    .ToList()
                    .SelectMany(w => w.TWarehousingDetails
                        .Where(d => string.IsNullOrWhiteSpace(productName) ||
                            (d.Pr?.PrName?.Contains(
                                productName, StringComparison.OrdinalIgnoreCase) ?? false))
                        .Select(d => new ReceivingRow
                        {
                            WaId = w.WaId,
                            HaId = w.HaId,
                            EmId = w.EmId,
                            WaDate = w.WaDate,
                            WaShelfFlag = w.WaShelfFlag,
                            WaFlag = w.WaFlag,
                            WaHidden = w.WaHidden ?? string.Empty,
                            WaDetailId = d.WaDetailId,
                            PrId = d.PrId,
                            ProductName = d.Pr?.PrName ?? "（商品名なし）",
                            WaQuantity = d.WaQuantity,
                            EmployeeName = w.Em?.EmName ?? "未設定",
                            FlagDisplay = w.WaFlag == HiddenFlag
                                ? "非表示 (2)"
                                : w.WaShelfFlag == ShelvedFlag
                                    ? "棚入れ済み"
                                    : "未棚入れ"
                        }))
                    .ToList();

                dg_Warehousings.ItemsSource = rows;
                txt_RecordCount.Text = $"表示件数: {rows.Count} 件";
            }
            catch (Exception ex)
            {
                ShowError("入庫検索に失敗しました。\n" + ex.Message);
            }
        }

        /// <summary>
        /// 検索条件をクリアする
        /// </summary>
        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchWaId.Clear();
            txt_SearchHaId.Clear();
            txt_SearchProductName.Clear();
            chk_IncludeHidden.IsChecked = false;
            LoadWarehousings();
        }

        /// <summary>
        /// 新規入庫登録画面を開く
        /// </summary>
        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            _editingWaId = null;
            _editingWaDetailId = null;

            txt_EditorTitle.Text = "新規入庫登録";
            btn_ExecuteEditor.Content = "登録実行";
            btn_ExecuteEditor.Background = System.Windows.Media.Brushes.Teal;

            LoadEditorChoices();

            dp_WaDate.SelectedDate = DateTime.Today;
            txt_WaQuantity.Text = "1";
            cmb_HaId.SelectedIndex = -1;
            cmb_Product.ItemsSource = null;
            cmb_Employee.SelectedIndex = -1;

            modal_Editor.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 選択した入庫情報を編集する
        /// </summary>
        private void btn_EditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Warehousings.SelectedItem is not ReceivingRow selected)
            {
                ShowWarning("編集する入庫情報を一覧から選択してください。");
                return;
            }

            _isEditing = true;
            _editingWaId = selected.WaId;
            _editingWaDetailId = selected.WaDetailId;

            txt_EditorTitle.Text = $"入庫情報編集（入庫ID: {selected.WaId}）";
            btn_ExecuteEditor.Content = "更新保存";
            btn_ExecuteEditor.Background = System.Windows.Media.Brushes.SteelBlue;

            LoadEditorChoices();
            cmb_HaId.SelectedValue = selected.HaId;
            LoadProductsForOrder(selected.HaId);
            cmb_Product.SelectedValue = selected.PrId;
            cmb_Employee.SelectedValue = selected.EmId;
            dp_WaDate.SelectedDate = selected.WaDate.Date;
            txt_WaQuantity.Text = selected.WaQuantity.ToString();

            modal_Editor.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 発注・社員の選択肢を読み込む
        /// </summary>
        private void LoadEditorChoices()
        {
            try
            {
                using var context = new SalesManagementContext();
                _loadingCombo = true;

                cmb_HaId.ItemsSource = context.THattyus
                    .AsNoTracking()
                    .Include(h => h.Ma)
                    .Where(h => h.HaFlag == ActiveFlag)
                    .OrderByDescending(h => h.HaDate)
                    .Select(h => new PurchaseChoice
                    {
                        HaId = h.HaId,
                        HaDate = h.HaDate,
                        MakerName = h.Ma.MaName
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
                ShowError("発注・社員の選択肢を取得できませんでした。\n" + ex.Message);
            }
        }

        /// <summary>
        /// 発注ID選択時に発注明細の商品を読み込む
        /// </summary>
        private void cmb_HaId_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_loadingCombo || cmb_HaId.SelectedValue == null ||
                !int.TryParse(cmb_HaId.SelectedValue.ToString(), out int haId))
                return;

            LoadProductsForOrder(haId);
        }

        private void LoadProductsForOrder(int haId)
        {
            try
            {
                using var context = new SalesManagementContext();

                var products = context.THattyuDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.HaId == haId)
                    .OrderBy(d => d.PrId)
                    .Select(d => new ProductChoice
                    {
                        PrId = d.PrId,
                        PrName = d.Pr.PrName,
                        OrderedQuantity = d.HaQuantity
                    })
                    .ToList();

                cmb_Product.ItemsSource = products;
                if (products.Count == 1 && !_isEditing)
                    cmb_Product.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError("発注明細の商品を取得できませんでした。\n" + ex.Message);
            }
        }

        /// <summary>
        /// 入庫登録・更新を実行する。
        /// 登録実行を棚入れ確定として扱い、在庫数へ反映する。
        /// </summary>
        private void btn_ExecuteEditor_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_HaId.SelectedValue == null ||
                !int.TryParse(cmb_HaId.SelectedValue.ToString(), out int haId))
            {
                ShowWarning("発注を選択してください。");
                return;
            }

            if (dp_WaDate.SelectedDate == null)
            {
                ShowWarning("入庫日を指定してください。");
                return;
            }

            if (cmb_Product.SelectedValue == null ||
                !int.TryParse(cmb_Product.SelectedValue.ToString(), out int prId))
            {
                ShowWarning("選択した発注に含まれる商品を選択してください。");
                return;
            }

            if (!int.TryParse(txt_WaQuantity.Text, out int quantity) || quantity <= 0)
            {
                ShowWarning("入庫数量には1以上の整数を入力してください。");
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

                var purchaseOrder = context.THattyus.FirstOrDefault(h => h.HaId == haId);
                if (purchaseOrder == null)
                {
                    ShowWarning("選択した発注情報が見つかりません。");
                    return;
                }

                var purchaseDetail = context.THattyuDetails
                    .FirstOrDefault(d => d.HaId == haId && d.PrId == prId);
                if (purchaseDetail == null)
                {
                    ShowWarning("選択した商品は発注明細に存在しません。");
                    return;
                }

                int? oldWaId = _isEditing ? _editingWaId : null;
                int alreadyReceived = context.TWarehousingDetails
                    .Include(d => d.Wa)
                    .Where(d => d.Wa.HaId == haId && d.PrId == prId &&
                                (!oldWaId.HasValue || d.WaId != oldWaId.Value))
                    .Sum(d => (int?)d.WaQuantity) ?? 0;

                if (quantity > purchaseDetail.HaQuantity - alreadyReceived)
                {
                    ShowWarning($"発注数量を超えています。今回登録できる残り数量は {Math.Max(0, purchaseDetail.HaQuantity - alreadyReceived)} 個です。");
                    return;
                }

                if (_isEditing)
                {
                    if (!_editingWaId.HasValue || !_editingWaDetailId.HasValue)
                    {
                        ShowWarning("更新対象を特定できません。もう一度選択してください。");
                        return;
                    }

                    var warehousing = context.TWarehousings
                        .FirstOrDefault(w => w.WaId == _editingWaId.Value);
                    var detail = context.TWarehousingDetails
                        .FirstOrDefault(d => d.WaDetailId == _editingWaDetailId.Value);

                    if (warehousing == null || detail == null)
                    {
                        ShowWarning("更新対象の入庫情報が見つかりません。");
                        return;
                    }

                    // 既に棚入れ済みの旧数量を在庫から差し引き、新しい入庫数量を加算する。
                    if (warehousing.WaShelfFlag == ShelvedFlag)
                        ChangeStock(context, detail.PrId, -detail.WaQuantity);

                    warehousing.HaId = haId;
                    warehousing.EmId = emId;
                    warehousing.WaDate = dp_WaDate.SelectedDate.Value.Date;
                    warehousing.WaShelfFlag = ShelvedFlag;
                    warehousing.WaFlag = ActiveFlag;
                    detail.PrId = prId;
                    detail.WaQuantity = quantity;

                    ChangeStock(context, prId, quantity);
                    context.SaveChanges();
                    transaction.Commit();
                    ShowInfo("入庫情報を更新し、在庫数に反映しました。");
                }
                else
                {
                    var warehousing = new TWarehousing
                    {
                        HaId = haId,
                        EmId = emId,
                        WaDate = dp_WaDate.SelectedDate.Value.Date,
                        WaShelfFlag = ShelvedFlag,
                        WaHidden = null,
                        WaFlag = ActiveFlag
                    };

                    warehousing.TWarehousingDetails.Add(new TWarehousingDetail
                    {
                        PrId = prId,
                        WaQuantity = quantity
                    });

                    context.TWarehousings.Add(warehousing);
                    ChangeStock(context, prId, quantity);
                    context.SaveChanges();
                    transaction.Commit();
                    ShowInfo("入庫情報を登録し、棚入れ済みとして在庫数に反映しました。");
                }

                modal_Editor.Visibility = Visibility.Collapsed;
                LoadWarehousings();
            }
            catch (Exception ex)
            {
                ShowError("入庫情報の保存に失敗しました。\n" + ex.Message);
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
            {
                if (delta > 0)
                {
                    context.TStocks.Add(new TStock
                    {
                        PrId = prId,
                        StQuantity = delta,
                        StFlag = ActiveFlag
                    });
                    return;
                }

                throw new InvalidOperationException(
                    $"商品ID {prId} の有効な在庫レコードがありません。");
            }

            if (stock.StQuantity + delta < 0)
                throw new InvalidOperationException("在庫数がマイナスになるため処理できません。");

            stock.StQuantity += delta;
        }

        private void btn_HideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Warehousings.SelectedItem is not ReceivingRow selected)
            {
                ShowWarning("非表示にする入庫情報を一覧から選択してください。");
                return;
            }

            hide_txt_TargetInfo.Text =
                $"入庫ID: {selected.WaId} / 発注ID: {selected.HaId} / 商品: {selected.ProductName}";
            hide_txt_Reason.Text = selected.WaHidden;
            modal_Hide.Tag = selected.WaId;
            modal_Hide.Visibility = Visibility.Visible;

            /*if (dg_Warehousings.SelectedItem is ReceivingRow selected)
            {
                hide_txt_TargetInfo.Text =
                    $"入庫: [{selected.WaId}] 発注ID: {selected.HaId} ／ 商品: {selected.ProductName}";

                hide_txt_Reason.Text = string.Empty;

                modal_Hide.Tag = selected.WaId;
                modal_Hide.Visibility = Visibility.Visible;

                hide_txt_Reason.Focus();
            }*/
        }

        private void btn_ExecuteHide_Click(object sender, RoutedEventArgs e)
        {
            if (modal_Hide.Tag is not int waId)
            {
                ShowWarning("非表示対象を特定できません。");
                return;
            }

            try
            {
                using var context = new SalesManagementContext();
                var warehousing = context.TWarehousings
                    .FirstOrDefault(w => w.WaId == waId);

                if (warehousing == null)
                {
                    ShowWarning("対象の入庫情報が見つかりません。");
                    return;
                }

                warehousing.WaFlag = HiddenFlag;
                warehousing.WaHidden = string.IsNullOrWhiteSpace(hide_txt_Reason.Text)
                    ? warehousing.WaHidden
                    : hide_txt_Reason.Text.Trim();

                context.SaveChanges();
                modal_Hide.Visibility = Visibility.Collapsed;
                ShowInfo("入庫情報を非表示にしました。");
                LoadWarehousings();
            }
            catch (Exception ex)
            {
                ShowError("入庫情報の非表示に失敗しました。\n" + ex.Message);
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
            MessageBox.Show(message, "入庫管理", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowError(string message) =>
            MessageBox.Show(message, "入庫管理", MessageBoxButton.OK, MessageBoxImage.Error);

        private static void ShowInfo(string message) =>
            MessageBox.Show(message, "入庫管理", MessageBoxButton.OK, MessageBoxImage.Information);

        private sealed class ReceivingRow
        {
            public int WaId { get; set; }
            public int HaId { get; set; }
            public int? EmId { get; set; }
            public DateTime WaDate { get; set; }
            public int? WaShelfFlag { get; set; }
            public int WaFlag { get; set; }
            public string WaHidden { get; set; } = string.Empty;
            public int WaDetailId { get; set; }
            public int PrId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int WaQuantity { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string FlagDisplay { get; set; } = string.Empty;
        }

        private sealed class PurchaseChoice
        {
            public int HaId { get; set; }
            public DateTime HaDate { get; set; }
            public string MakerName { get; set; } = string.Empty;
            public string DisplayText => $"発注ID: {HaId} / {HaDate:yyyy/MM/dd} / {MakerName}";
        }

        private sealed class ProductChoice
        {
            public int PrId { get; set; }
            public string PrName { get; set; } = string.Empty;
            public int OrderedQuantity { get; set; }
            public string DisplayText => $"{PrId} / {PrName}（発注数量: {OrderedQuantity}）";
        }
    }
}
