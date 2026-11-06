

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 機能12 出庫管理。
    /// TSyukko（出庫ヘッダー）とTSyukkoDetail（商品・数量明細）を操作する。
    /// </summary>
    public partial class Shipping : UserControl
    {
        private const int ActiveFlag = 0;
        private const int HiddenFlag = 2;

        private bool _isEditing;
        private int? _editingSyId;
        private int? _editingSyDetailId;
        private bool _loadingCombo;

        public Shipping()
        {
            InitializeComponent();
            LoadShippings();
        }

        /// <summary>
        /// 出庫一覧を読み込む
        /// </summary>
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
        /// 出庫IDの入力内容を受注ID欄に自動コピーする
        /// </summary>
        private void txt_SearchSyId_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (txt_SearchOrId.Text != txt_SearchSyId.Text)
            {
                txt_SearchOrId.Text = txt_SearchSyId.Text;
                txt_SearchOrId.CaretIndex =
                    txt_SearchOrId.Text.Length;
            }
        }

        /// <summary>
        /// 出庫情報を検索する
        /// </summary>
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

                string productName =
                    txt_SearchProductName.Text?.Trim() ?? string.Empty;

                var rows = query
                    .OrderByDescending(s => s.SyDate)
                    .ThenByDescending(s => s.SyId)
                    .ToList()
                    .SelectMany(s => s.TSyukkoDetails
                        .Where(d =>
                            string.IsNullOrWhiteSpace(productName) ||
                            (d.Pr?.PrName?.Contains(
                                productName,
                                StringComparison.OrdinalIgnoreCase) ?? false))
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

        /// <summary>
        /// 検索条件をクリアする
        /// </summary>
        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchSyId.Clear();
            txt_SearchOrId.Clear();
            txt_SearchProductName.Clear();

            chk_IncludeHidden.IsChecked = false;

            LoadShippings();
        }

        /// <summary>
        /// 新規出庫登録画面を開く
        /// </summary>
        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            _editingSyId = null;
            _editingSyDetailId = null;

            txt_EditorTitle.Text = "新規出庫登録";
            btn_ExecuteEditor.Content = "登録実行";
            btn_ExecuteEditor.Background =
                System.Windows.Media.Brushes.Teal;

            LoadEditorChoices();

            dp_SyDate.SelectedDate = DateTime.Today;
            txt_SyQuantity.Text = "1";

            cmb_OrId.SelectedIndex = -1;
            cmb_Product.ItemsSource = null;
            cmb_Employee.SelectedIndex = -1;

            modal_Editor.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 選択した出庫情報を編集する
        /// </summary>
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

            txt_EditorTitle.Text =
                $"出庫情報編集（出庫ID: {selected.SyId}）";

            btn_ExecuteEditor.Content = "更新保存";
            btn_ExecuteEditor.Background =
                System.Windows.Media.Brushes.SteelBlue;

            LoadEditorChoices();

            cmb_OrId.SelectedValue = selected.OrId;

            LoadProductsForOrder(selected.OrId);

            cmb_Product.SelectedValue = selected.PrId;
            cmb_Employee.SelectedValue = selected.EmId;

            dp_SyDate.SelectedDate =
                selected.SyDate?.Date ?? DateTime.Today;

            txt_SyQuantity.Text = selected.SyQuantity.ToString();

            modal_Editor.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 登録・編集画面の選択肢を読み込む
        /// </summary>
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

                ShowError(
                    "受注・社員の選択肢を取得できませんでした。\n"
                    + ex.Message);
            }
        }

        /// <summary>
        /// 受注ID選択時に受注明細の商品を読み込む
        /// </summary>
        private void cmb_OrId_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_loadingCombo ||
                cmb_OrId.SelectedValue == null ||
                !int.TryParse(
                    cmb_OrId.SelectedValue.ToString(),
                    out int orId))
            {
                return;
            }

            LoadProductsForOrder(orId);
        }

        /// <summary>
        /// 選択した受注に含まれる商品を読み込む
        /// </summary>
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
                ShowError(
                    "受注明細の商品を取得できませんでした。\n"
                    + ex.Message);
            }
        }

        /// <summary>
        /// 出庫情報を登録・更新する
        /// </summary>
        private void btn_ExecuteEditor_Click(
            object sender, RoutedEventArgs e)
        {
            if (cmb_OrId.SelectedValue == null ||
                !int.TryParse(
                    cmb_OrId.SelectedValue.ToString(),
                    out int orId))
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
                !int.TryParse(
                    cmb_Product.SelectedValue.ToString(),
                    out int prId))
            {
                ShowWarning("選択した受注に含まれる商品を選択してください。");
                return;
            }

            if (!int.TryParse(txt_SyQuantity.Text, out int quantity) ||
                quantity <= 0)
            {
                ShowWarning("出庫数量には1以上の整数を入力してください。");
                return;
            }

            int? emId = null;

            if (cmb_Employee.SelectedValue != null &&
                int.TryParse(
                    cmb_Employee.SelectedValue.ToString(),
                    out int selectedEmId))
            {
                emId = selectedEmId;
            }

            try
            {
                using var context = new SalesManagementContext();

                var order = context.TOrders
                    .FirstOrDefault(o => o.OrId == orId);

                if (order == null)
                {
                    ShowWarning("選択した受注情報が見つかりません。");
                    return;
                }

                var orderDetail = context.TOrderDetails
                    .FirstOrDefault(d =>
                        d.OrId == orId && d.PrId == prId);

                if (orderDetail == null)
                {
                    ShowWarning("選択した商品は受注明細に存在しません。");
                    return;
                }

                if (_isEditing)
                {
                    if (!_editingSyId.HasValue ||
                        !_editingSyDetailId.HasValue)
                    {
                        ShowWarning(
                            "更新対象を特定できません。もう一度選択してください。");
                        return;
                    }

                    var shipping = context.TSyukkos
                        .FirstOrDefault(s =>
                            s.SyId == _editingSyId.Value);

                    var detail = context.TSyukkoDetails
                        .FirstOrDefault(d =>
                            d.SyDetailId == _editingSyDetailId.Value);

                    if (shipping == null || detail == null)
                    {
                        ShowWarning("更新対象の出庫情報が見つかりません。");
                        return;
                    }

                    shipping.OrId = order.OrId;
                    shipping.ClId = order.ClId;
                    shipping.SoId = order.SoId;
                    shipping.EmId = emId;
                    shipping.SyDate =
                        dp_SyDate.SelectedDate.Value.Date;

                    detail.SyId = shipping.SyId;
                    detail.PrId = prId;
                    detail.SyQuantity = quantity;

                    context.SaveChanges();

                    ShowInfo("出庫情報を更新しました。");
                }
                else
                {
                    var shipping = new TSyukko
                    {
                        OrId = order.OrId,
                        ClId = order.ClId,
                        SoId = order.SoId,
                        EmId = emId,
                        SyDate =
                            dp_SyDate.SelectedDate.Value.Date,
                        SyStateFlag = 0,
                        SyHidden = null,
                        SyFlag = ActiveFlag
                    };

                    shipping.TSyukkoDetails.Add(
                        new TSyukkoDetail
                        {
                            PrId = prId,
                            SyQuantity = quantity
                        });

                    context.TSyukkos.Add(shipping);

                    context.SaveChanges();

                    ShowInfo("出庫情報を登録しました。");
                }

                modal_Editor.Visibility = Visibility.Collapsed;

                LoadShippings();
            }
            catch (Exception ex)
            {
                ShowError(
                    "出庫情報の保存に失敗しました。\n"
                    + ex.Message);
            }
        }

        /// <summary>
        /// 選択した出庫情報の非表示画面を開く
        /// </summary>
        private void btn_HideSelected_Click(
            object sender, RoutedEventArgs e)
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

        /// <summary>
        /// 出庫情報を非表示にする
        /// </summary>
        private void btn_ExecuteHide_Click(
            object sender, RoutedEventArgs e)
        {
            if (modal_Hide.Tag is not int syId)
            {
                ShowWarning("非表示対象を特定できません。");
                return;
            }

            try
            {
                using var context = new SalesManagementContext();

                var shipping = context.TSyukkos
                    .FirstOrDefault(s => s.SyId == syId);

                if (shipping == null)
                {
                    ShowWarning("対象の出庫情報が見つかりません。");
                    return;
                }

                shipping.SyFlag = HiddenFlag;

                if (!string.IsNullOrWhiteSpace(hide_txt_Reason.Text))
                {
                    shipping.SyHidden =
                        hide_txt_Reason.Text.Trim();
                }

                context.SaveChanges();

                modal_Hide.Visibility = Visibility.Collapsed;

                ShowInfo("出庫情報を非表示にしました。");

                LoadShippings();
            }
            catch (Exception ex)
            {
                ShowError(
                    "出庫情報の非表示に失敗しました。\n"
                    + ex.Message);
            }
        }

        /// <summary>
        /// 登録・編集画面を閉じる
        /// </summary>
        private void btn_CancelEditor_Click(
            object sender, RoutedEventArgs e)
        {
            modal_Editor.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 登録・編集画面の背景クリックで閉じる
        /// </summary>
        private void modal_Editor_Backdrop_Click(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            modal_Editor.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 非表示画面を閉じる
        /// </summary>
        private void btn_CancelHide_Click(
            object sender, RoutedEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 非表示画面の背景クリックで閉じる
        /// </summary>
        private void modal_Hide_Backdrop_Click(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 任意入力の整数を検証する
        /// </summary>
        private static bool TryParseOptionalInt(
            string? text,
            out int? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(text))
                return true;

            if (!int.TryParse(text.Trim(), out int parsed) ||
                parsed < 1)
            {
                return false;
            }

            value = parsed;
            return true;
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                "出庫管理",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(
                message,
                "出庫管理",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                "出庫管理",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// 出庫一覧表示用クラス
        /// </summary>
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

        /// <summary>
        /// 受注選択用クラス
        /// </summary>
        private sealed class OrderChoice
        {
            public int OrId { get; set; }
            public DateTime OrDate { get; set; }
            public int ClId { get; set; }
            public int SoId { get; set; }
            public string ClientName { get; set; } = string.Empty;

            public string DisplayText =>
                $"受注ID: {OrId} / {OrDate:yyyy/MM/dd} / {ClientName}";
        }

        /// <summary>
        /// 商品選択用クラス
        /// </summary>
        private sealed class ProductChoice
        {
            public int PrId { get; set; }
            public string PrName { get; set; } = string.Empty;
            public int OrderedQuantity { get; set; }

            public string DisplayText =>
                $"{PrId} / {PrName}（受注数量: {OrderedQuantity}）";
        }
    }
}
