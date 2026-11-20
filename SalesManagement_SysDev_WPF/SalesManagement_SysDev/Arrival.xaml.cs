using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    // =========================================================================
    // 入荷管理 画面表示用モデル
    // =========================================================================
    public class ArrivalDisplayModel
    {
        public int ArId { get; set; }
        public int OrId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string SalesOfficeName { get; set; } = string.Empty;
        public int? PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public string MaName { get; set; } = string.Empty;
        public int? ArQuantity { get; set; }
        public string ArQuantityFormatted => ArQuantity.HasValue ? $"{ArQuantity:N0} 個" : "-";
        public decimal Price { get; set; }
        public string PriceFormatted => $"{Price:N0} 円";
        public DateTime? ArDate { get; set; }
        public string ArDateFormatted => ArDate.HasValue ? ArDate.Value.ToString("yyyy/MM/dd") : "-";
        public int? ArStateFlag { get; set; }
        public string StateFormatted => ArStateFlag switch
        {
            1 => "検品済",
            _ => "未検品"
        };
        public int ArFlag { get; set; }
        public string ArFlagFormatted => ArFlag switch
        {
            0 => "通常",
            2 => "非表示",
            _ => $"その他({ArFlag})"
        };
        public string? ArHidden { get; set; }
        // For detail lookup
        public int ArDetailId { get; set; }
    }

    /// <summary>
    /// 入荷登録用 受注選択ComboBoxモデル
    /// </summary>
    public class ArrivalOrderSelectModel
    {
        public int OrId { get; set; }
        public int ClId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int SoId { get; set; }
        public string SalesOfficeName { get; set; } = string.Empty;
        public DateTime OrDate { get; set; }
        public List<int> ProductIds { get; set; } = new List<int>();
        public string DisplayText => $"受注ID: {OrId} / {ClientName} ({SalesOfficeName}) / {OrDate:yyyy/MM/dd}";
    }

    // =========================================================================
    // 商品選択ComboBox用モデル
    // =========================================================================
    public class ProductSelectModel
    {
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public string MaName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string DisplayText => $"[{PrId}] {PrName} ({MaName})";
    }

    // =========================================================================
    // 入荷管理サービス
    // =========================================================================
    public class ArrivalService
    {
        /// <summary>
        /// 全メーカー取得
        /// </summary>
        public List<MMaker> GetMakers()
        {
            try
            {
                using var ctx = new SalesManagementContext();
                return ctx.MMakers.OrderBy(m => m.MaId).ToList();
            }
            catch
            {
                return new List<MMaker>();
            }
        }

        /// <summary>
        /// 受注選択リスト取得（登録モーダル用）
        /// </summary>
        public List<ArrivalOrderSelectModel> GetOrderSelectList()
        {
            try
            {
                using var ctx = new SalesManagementContext();
                return ctx.TOrders
                    .AsNoTracking()
                    .Include(o => o.Cl)
                    .Include(o => o.So)
                    .Include(o => o.TOrderDetails)
                    .Where(o => o.OrFlag != 2)
                    .OrderByDescending(o => o.OrId)
                    .Select(o => new ArrivalOrderSelectModel
                    {
                        OrId = o.OrId,
                        ClId = o.ClId,
                        ClientName = o.Cl.ClName,
                        SoId = o.SoId,
                        SalesOfficeName = o.So.SoName,
                        OrDate = o.OrDate,
                        ProductIds = o.TOrderDetails.Select(d => d.PrId).ToList()
                    })
                    .ToList();
            }
            catch
            {
                return new List<ArrivalOrderSelectModel>();
            }
        }

        /// <summary>
        /// 商品選択リスト取得（登録・更新モーダル用）
        /// </summary>
        public List<ProductSelectModel> GetProductSelectList()
        {
            try
            {
                using var ctx = new SalesManagementContext();
                return ctx.MProducts
                    .Include(p => p.Ma)
                    .Where(p => p.PrFlag == 0)
                    .OrderBy(p => p.PrId)
                    .Select(p => new ProductSelectModel
                    {
                        PrId = p.PrId,
                        PrName = p.PrName,
                        MaName = p.Ma.MaName,
                        Price = p.Price
                    })
                    .ToList();
            }
            catch
            {
                return new List<ProductSelectModel>();
            }
        }

        /// <summary>
        /// 入荷一覧検索（T_ArrivalDetail + T_Arrival + MProduct + MMaker JOIN）
        /// </summary>
        public List<ArrivalDisplayModel> SearchArrivals(
            int? arId, int? prId, string? prName, int? maId,
            DateTime? dateFrom, DateTime? dateTo,
            bool includeHidden)
        {
            try
            {
                using var ctx = new SalesManagementContext();

                var query = ctx.TArrivalDetails
                    .Include(d => d.Ar).ThenInclude(a => a!.Or).ThenInclude(o => o.Cl)
                    .Include(d => d.Ar).ThenInclude(a => a!.Or).ThenInclude(o => o.So)
                    .Include(d => d.Ar).ThenInclude(a => a!.Cl)
                    .Include(d => d.Ar).ThenInclude(a => a!.So)
                    .Include(d => d.Pr).ThenInclude(p => p!.Ma)
                    .AsQueryable();

                // 入荷IDフィルタ
                if (arId.HasValue)
                    query = query.Where(d => d.ArId == arId.Value);

                // 商品IDフィルタ
                if (prId.HasValue)
                    query = query.Where(d => d.PrId == prId.Value);

                // 商品名フィルタ
                if (!string.IsNullOrWhiteSpace(prName))
                    query = query.Where(d => d.Pr != null && d.Pr.PrName.Contains(prName));

                // メーカーフィルタ
                if (maId.HasValue && maId.Value > 0)
                    query = query.Where(d => d.Pr != null && d.Pr.MaId == maId.Value);

                // 入荷日フィルタ
                if (dateFrom.HasValue)
                    query = query.Where(d => d.Ar != null && d.Ar.ArDate >= dateFrom.Value);
                if (dateTo.HasValue)
                    query = query.Where(d => d.Ar != null && d.Ar.ArDate <= dateTo.Value);

                // 非表示フィルタ
                if (!includeHidden)
                    query = query.Where(d => d.Ar == null || d.Ar.ArFlag != 2);

                var list = query
                    .OrderBy(d => d.ArId)
                    .ThenBy(d => d.ArDetailId)
                    .ToList();

                return list.Select(d => new ArrivalDisplayModel
                {
                    ArId = d.ArId ?? 0,
                    ArDetailId = d.ArDetailId,
                    OrId = d.Ar?.OrId ?? 0,
                    ClientName = d.Ar?.Cl?.ClName ?? d.Ar?.Or?.Cl?.ClName ?? "-",
                    SalesOfficeName = d.Ar?.So?.SoName ?? d.Ar?.Or?.So?.SoName ?? "-",
                    PrId = d.PrId,
                    PrName = d.Pr?.PrName ?? "-",
                    MaName = d.Pr?.Ma?.MaName ?? "-",
                    ArQuantity = d.ArQuantity,
                    Price = d.Pr?.Price ?? 0,
                    ArDate = d.Ar?.ArDate,
                    ArStateFlag = d.Ar?.ArStateFlag,
                    ArFlag = d.Ar?.ArFlag ?? 0,
                    ArHidden = d.Ar?.ArHidden
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"検索エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ArrivalDisplayModel>();
            }
        }

        /// <summary>
        /// 入荷明細1件取得（更新・非表示モーダル用）
        /// </summary>
        public ArrivalDisplayModel? GetArrivalDetailById(int arDetailId)
        {
            try
            {
                using var ctx = new SalesManagementContext();
                var d = ctx.TArrivalDetails
                    .Include(x => x.Ar).ThenInclude(a => a!.Cl)
                    .Include(x => x.Ar).ThenInclude(a => a!.So)
                    .Include(x => x.Pr).ThenInclude(p => p!.Ma)
                    .FirstOrDefault(x => x.ArDetailId == arDetailId);

                if (d == null) return null;

                return new ArrivalDisplayModel
                {
                    ArId = d.ArId ?? 0,
                    ArDetailId = d.ArDetailId,
                    OrId = d.Ar?.OrId ?? 0,
                    ClientName = d.Ar?.Cl?.ClName ?? "-",
                    SalesOfficeName = d.Ar?.So?.SoName ?? "-",
                    PrId = d.PrId,
                    PrName = d.Pr?.PrName ?? "-",
                    MaName = d.Pr?.Ma?.MaName ?? "-",
                    ArQuantity = d.ArQuantity,
                    Price = d.Pr?.Price ?? 0,
                    ArDate = d.Ar?.ArDate,
                    ArStateFlag = d.Ar?.ArStateFlag,
                    ArFlag = d.Ar?.ArFlag ?? 0,
                    ArHidden = d.Ar?.ArHidden
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 新規入荷登録（T_Arrival + T_ArrivalDetail を同時作成）
        /// 受注IDが指定されている場合は、その受注の顧客・営業所情報を引き継ぐ
        /// </summary>
        public (bool Success, string Message) RegisterArrival(
            int? orId, int prId, int quantity, DateTime arDate, int arFlag, string? arHidden)
        {
            if (prId <= 0)
                return (false, "商品を選択してください。");
            if (quantity <= 0)
                return (false, "入荷数量は1以上を入力してください。");

            try
            {
                using var ctx = new SalesManagementContext();

                TOrder? order = null;
                if (orId.HasValue && orId.Value > 0)
                {
                    order = ctx.TOrders.FirstOrDefault(o => o.OrId == orId.Value);
                }

                int soId = order?.SoId ?? ctx.MSalesOffices.Select(s => s.SoId).FirstOrDefault();
                int clId = order?.ClId ?? ctx.MClients.Select(c => c.ClId).FirstOrDefault();
                int finalOrId = order?.OrId ?? ctx.TOrders.Select(o => o.OrId).FirstOrDefault();

                if (soId == 0 || clId == 0 || finalOrId == 0)
                    return (false, "関連マスタ（営業所・顧客・受注）が存在しません。先にデータを登録してください。");

                var arrival = new TArrival
                {
                    SoId = soId,
                    ClId = clId,
                    OrId = finalOrId,
                    ArDate = arDate,
                    ArStateFlag = 0,
                    ArFlag = arFlag,
                    ArHidden = arHidden
                };
                ctx.TArrivals.Add(arrival);
                ctx.SaveChanges(); // ArId が確定する

                var detail = new TArrivalDetail
                {
                    ArId = arrival.ArId,
                    PrId = prId,
                    ArQuantity = quantity
                };
                ctx.TArrivalDetails.Add(detail);
                ctx.SaveChanges();

                return (true, $"入荷ID: {arrival.ArId} を登録しました。(受注ID: {finalOrId})");
            }
            catch (Exception ex)
            {
                return (false, $"登録エラー: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        /// <summary>
        /// 既存互換用
        /// </summary>
        public (bool Success, string Message) RegisterArrival(
            int prId, int quantity, DateTime arDate, int arFlag, string? arHidden)
            => RegisterArrival(null, prId, quantity, arDate, arFlag, arHidden);

        /// <summary>
        /// 入荷検品確定（ArStateFlag を 0 → 1 に更新）
        /// </summary>
        public (bool Success, string Message) ConfirmArrival(int arId)
        {
            if (arId <= 0)
                return (false, "確定対象の入荷IDが正しくありません。");

            try
            {
                using var ctx = new SalesManagementContext();
                var arrival = ctx.TArrivals.FirstOrDefault(a => a.ArId == arId);
                if (arrival == null)
                    return (false, $"入荷ID: {arId} のデータが見つかりません。");

                if (arrival.ArStateFlag == 1)
                    return (false, $"入荷ID: {arId} は既に検品確定済みです。");

                arrival.ArStateFlag = 1; // 検品確定
                ctx.SaveChanges();

                return (true, $"入荷ID: {arId} の検品を確定しました。");
            }
            catch (Exception ex)
            {
                return (false, $"確定エラー: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        /// <summary>
        /// 入荷情報更新（明細行単位）
        /// </summary>
        public (bool Success, string Message) UpdateArrival(
            int arDetailId, int prId, int quantity, DateTime? arDate, int arFlag, string? arHidden)
        {
            if (prId <= 0)
                return (false, "商品を選択してください。");
            if (quantity <= 0)
                return (false, "入荷数量は1以上を入力してください。");

            try
            {
                using var ctx = new SalesManagementContext();

                var detail = ctx.TArrivalDetails.Include(d => d.Ar).FirstOrDefault(d => d.ArDetailId == arDetailId);
                if (detail == null)
                    return (false, $"入荷明細ID: {arDetailId} が見つかりません。");

                detail.PrId = prId;
                detail.ArQuantity = quantity;

                if (detail.Ar != null)
                {
                    detail.Ar.ArDate = arDate;
                    detail.Ar.ArFlag = arFlag;
                    detail.Ar.ArHidden = arHidden;
                }

                ctx.SaveChanges();
                return (true, $"入荷明細ID: {arDetailId} を更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"更新エラー: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        /// <summary>
        /// 入荷非表示設定（ArFlag を 0 → 2 に更新）
        /// </summary>
        public (bool Success, string Message) SetArrivalHiddenFlag(int arId, string reason)
        {
            try
            {
                using var ctx = new SalesManagementContext();
                var arrival = ctx.TArrivals.FirstOrDefault(a => a.ArId == arId);
                if (arrival == null)
                    return (false, $"入荷ID: {arId} が見つかりません。");

                arrival.ArFlag = 2;
                arrival.ArHidden = string.IsNullOrWhiteSpace(reason) ? "管理者による非表示設定" : reason;
                ctx.SaveChanges();

                return (true, $"入荷ID: {arId} を非表示に設定しました。");
            }
            catch (Exception ex)
            {
                return (false, $"非表示設定エラー: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        /// <summary>
        /// 入荷物理削除
        /// </summary>
        public (bool Success, string Message) DeleteArrival(int arId)
        {
            if (arId <= 0)
                return (false, "削除対象の入荷IDが正しくありません。");

            try
            {
                using var ctx = new SalesManagementContext();
                var arrival = ctx.TArrivals.FirstOrDefault(a => a.ArId == arId);
                if (arrival == null)
                    return (false, $"入荷ID: {arId} のデータが見つかりません。");

                var details = ctx.TArrivalDetails.Where(d => d.ArId == arId).ToList();
                ctx.TArrivalDetails.RemoveRange(details);
                ctx.TArrivals.Remove(arrival);
                ctx.SaveChanges();

                return (true, $"入荷ID: {arId} のデータを削除しました。");
            }
            catch (Exception ex)
            {
                return (false, $"削除エラー: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }

    // =========================================================================
    // Arrival.xaml コードビハインド
    // =========================================================================
    public partial class Arrival : UserControl
    {
        private readonly ArrivalService _service = new();
        private List<ArrivalDisplayModel> _cachedList = new();

        public Arrival()
        {
            InitializeComponent();
            Loaded += Arrival_Loaded;
        }

        private void Arrival_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMakers();
            ExecuteSearch();
            UpdateActionButtonState();
        }

        // =====================================================================
        // マスタ読み込み
        // =====================================================================
        private void LoadMakers()
        {
            var makers = _service.GetMakers();
            var all = new List<MMaker> { new MMaker { MaId = 0, MaName = "すべてのメーカー/仕入先" } };
            all.AddRange(makers);
            cmb_SearchMaker.ItemsSource = all;
            cmb_SearchMaker.SelectedIndex = 0;
        }

        // =====================================================================
        // 検索
        // =====================================================================
        private void ExecuteSearch()
        {
            int? arId = null;
            if (int.TryParse(txt_SearchArId.Text.Trim(), out int aid) && aid > 0) arId = aid;

            int? prId = null;
            if (int.TryParse(txt_SearchPrId.Text.Trim(), out int pid) && pid > 0) prId = pid;

            string? prName = string.IsNullOrWhiteSpace(txt_SearchPrName.Text) ? null : txt_SearchPrName.Text.Trim();

            int? maId = null;
            if (cmb_SearchMaker.SelectedValue is int mId && mId > 0) maId = mId;

            DateTime? dateFrom = dp_SearchDateFrom.SelectedDate;
            DateTime? dateTo = dp_SearchDateTo.SelectedDate;

            // 状態フィルタ: cmb_SearchStatus の Tag で判定
            var statusItem = cmb_SearchStatus.SelectedItem as ComboBoxItem;
            bool includeHidden = chk_IncludeHidden.IsChecked == true;

            _cachedList = _service.SearchArrivals(arId, prId, prName, maId, dateFrom, dateTo, includeHidden);

            // 状態フィルタ（-1 = すべて）
            if (statusItem != null && statusItem.Tag?.ToString() != "-1")
            {
                if (int.TryParse(statusItem.Tag?.ToString(), out int flag))
                    _cachedList = _cachedList.Where(x => x.ArFlag == flag).ToList();
            }

            dg_Arrivals.ItemsSource = _cachedList;
            txt_ArrivalCount.Text = $"表示件数: {_cachedList.Count} 件";
            UpdateActionButtonState();
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e) => ExecuteSearch();

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchArId.Text = string.Empty;
            txt_SearchPrId.Text = string.Empty;
            txt_SearchPrName.Text = string.Empty;
            cmb_SearchMaker.SelectedIndex = 0;
            dp_SearchDateFrom.SelectedDate = null;
            dp_SearchDateTo.SelectedDate = null;
            cmb_SearchStatus.SelectedIndex = 0;
            chk_IncludeHidden.IsChecked = false;
            ExecuteSearch();
        }

        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ExecuteSearch();
        }

        private void chk_IncludeHidden_Click(object sender, RoutedEventArgs e) => ExecuteSearch();

        // =====================================================================
        // DataGrid イベント
        // =====================================================================
        private void UpdateActionButtonState()
        {
            var selected = dg_Arrivals.SelectedItem as ArrivalDisplayModel;
            bool has = selected != null;
            btn_DetailSelected.IsEnabled = has;
            btn_EditSelected.IsEnabled = has;
            btn_HideSelected.IsEnabled = has;
            btn_DeleteSelected.IsEnabled = has;
            btn_ConfirmArrival.IsEnabled = has && (selected?.ArStateFlag != 1);
        }

        private void btn_DetailSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Arrivals.SelectedItem is ArrivalDisplayModel selected)
            {
                OpenDetailModal(selected.ArId);
            }
        }

        private void OpenDetailModal(int arId)
        {
            try
            {
                using var context = new SalesManagementContext();
                var arrival = context.TArrivals
                    .AsNoTracking()
                    .Include(a => a.Cl)
                    .Include(a => a.So)
                    .FirstOrDefault(a => a.ArId == arId);

                if (arrival == null)
                {
                    MessageBox.Show("対象の入荷データが見つかりません。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                detail_txt_ArId.Text = $"入荷ID: {arrival.ArId} (受注ID: {arrival.OrId})";
                detail_txt_Client.Text = $"顧客名: {arrival.Cl?.ClName ?? $"顧客ID: {arrival.ClId}"}";
                detail_txt_Date.Text = $"入荷日: {arrival.ArDate:yyyy/MM/dd}";
                detail_txt_Office.Text = $"営業所: {arrival.So?.SoName ?? $"営業所ID: {arrival.SoId}"}";

                var details = context.TArrivalDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.ArId == arId)
                    .Select(d => new ArrivalDetailDisplayModel
                    {
                        ArDetailId = d.ArDetailId,
                        ArId = d.ArId,
                        PrId = d.PrId,
                        PrName = d.Pr != null ? d.Pr.PrName : $"商品ID: {d.PrId}",
                        ArQuantity = d.ArQuantity
                    })
                    .ToList();

                dg_ArrivalDetailItems.ItemsSource = details;
                modal_Detail.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("入荷詳細の取得に失敗しました。\n" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void btn_ConfirmArrival_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Arrivals.SelectedItem is not ArrivalDisplayModel selected)
            {
                MessageBox.Show("検品確定する入荷データを選択してください。", "検品確定", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selected.ArStateFlag == 1)
            {
                MessageBox.Show("選択された入荷は既に検品確定済みです。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"入荷ID: {selected.ArId} (商品: {selected.PrName}, 数量: {selected.ArQuantity}個) の検品を確定しますか？\n確定すると次の出荷工程へ進められます。",
                "検品確定確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var result = _service.ConfirmArrival(selected.ArId);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "検品確定完了", MessageBoxButton.OK, MessageBoxImage.Information);
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(result.Message, "確定エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dg_Arrivals_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateActionButtonState();

        private void dg_Arrivals_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_Arrivals.SelectedItem is ArrivalDisplayModel selected)
                OpenDetailModal(selected.ArId);
        }

        // =====================================================================
        // ツールバーボタン
        // =====================================================================
        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
            => OpenRegisterModal();

        private void btn_EditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Arrivals.SelectedItem is ArrivalDisplayModel selected)
                OpenUpdateModal(selected.ArDetailId);
            else
                MessageBox.Show("一覧から編集する入荷を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btn_HideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Arrivals.SelectedItem is ArrivalDisplayModel selected)
                OpenHideModal(selected);
            else
                MessageBox.Show("一覧から非表示にする入荷を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // =====================================================================
        // 入荷登録 モーダル
        // =====================================================================
        private void OpenRegisterModal()
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            var orders = _service.GetOrderSelectList();
            reg_cmb_Order.ItemsSource = orders;
            if (orders.Count > 0)
                reg_cmb_Order.SelectedIndex = 0;

            var products = _service.GetProductSelectList();
            reg_cmb_Product.ItemsSource = products;
            if (products.Count > 0) reg_cmb_Product.SelectedIndex = 0;

            reg_txt_Quantity.Text = "1";
            reg_dp_ArDate.SelectedDate = DateTime.Today;
            reg_cmb_Flag.SelectedIndex = 0;
            reg_txt_Hidden.Text = string.Empty;

            modal_ArrivalRegister.Visibility = Visibility.Visible;
            reg_txt_Quantity.Focus();
        }

        private void reg_cmb_Order_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reg_cmb_Order.SelectedItem is ArrivalOrderSelectModel order && order.ProductIds.Count > 0)
            {
                var products = _service.GetProductSelectList();
                // 受注に含まれる商品があれば優先選択
                var matched = products.FirstOrDefault(p => order.ProductIds.Contains(p.PrId));
                if (matched != null)
                {
                    reg_cmb_Product.SelectedValue = matched.PrId;
                }
            }
        }

        private void btn_CloseArrivalRegister_Click(object sender, RoutedEventArgs e)
            => modal_ArrivalRegister.Visibility = Visibility.Collapsed;

        private void modal_ArrivalRegister_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => modal_ArrivalRegister.Visibility = Visibility.Collapsed;

        private void btn_ExecuteArrivalRegister_Click(object sender, RoutedEventArgs e)
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            int? orId = reg_cmb_Order.SelectedValue as int?;

            if (reg_cmb_Product.SelectedValue is not int prId || prId <= 0)
            {
                ShowRegError("商品を選択してください。");
                return;
            }

            if (!int.TryParse(reg_txt_Quantity.Text.Trim(), out int qty) || qty <= 0)
            {
                ShowRegError("入荷数量は1以上の整数を入力してください。");
                return;
            }

            if (reg_dp_ArDate.SelectedDate == null)
            {
                ShowRegError("入荷日を選択してください。");
                return;
            }

            int flag = GetSelectedTag(reg_cmb_Flag);
            string? hidden = string.IsNullOrWhiteSpace(reg_txt_Hidden.Text) ? null : reg_txt_Hidden.Text.Trim();

            var result = _service.RegisterArrival(orId, prId, qty, reg_dp_ArDate.SelectedDate.Value, flag, hidden);
            if (result.Success)
            {
                ShowRegSuccess(result.Message);
                reg_txt_Quantity.Text = "1";
                reg_txt_Hidden.Text = string.Empty;
                ExecuteSearch();
            }
            else
            {
                ShowRegError(result.Message);
            }
        }

        private void ShowRegError(string msg)
        {
            txt_RegErrorMessage.Text = msg;
            border_RegErrorAlert.Visibility = Visibility.Visible;
        }

        private void ShowRegSuccess(string msg)
        {
            txt_RegSuccessMessage.Text = msg;
            border_RegSuccessAlert.Visibility = Visibility.Visible;
        }

        // =====================================================================
        // 入荷更新 モーダル
        // =====================================================================
        private void OpenUpdateModal(int arDetailId)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            var products = _service.GetProductSelectList();
            upd_cmb_Product.ItemsSource = products;

            upd_txt_SearchArId.Text = arDetailId.ToString();
            ClearUpdateForm();

            modal_ArrivalUpdate.Visibility = Visibility.Visible;

            if (arDetailId > 0)
                LoadArrivalForUpdate(arDetailId);
            else
                upd_txt_SearchArId.Focus();
        }

        private void ClearUpdateForm()
        {
            upd_txt_Quantity.Text = string.Empty;
            upd_dp_ArDate.SelectedDate = null;
            upd_cmb_Flag.SelectedIndex = 0;
            upd_txt_Hidden.Text = string.Empty;
        }

        private void upd_txt_SearchArId_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btn_LoadArrivalForUpdate_Click(sender, e);
        }

        private void btn_LoadArrivalForUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchArId.Text.Trim(), out int arDetailId) || arDetailId <= 0)
            {
                ShowUpdError("有効な入荷明細IDを半角数値で入力してください。");
                return;
            }
            LoadArrivalForUpdate(arDetailId);
        }

        private void LoadArrivalForUpdate(int arDetailId)
        {
            var data = _service.GetArrivalDetailById(arDetailId);
            if (data == null)
            {
                ShowUpdError($"入荷明細ID: {arDetailId} のデータが見つかりません。");
                ClearUpdateForm();
                return;
            }

            upd_txt_SearchArId.Text = data.ArDetailId.ToString();
            upd_cmb_Product.SelectedValue = data.PrId;
            upd_txt_Quantity.Text = data.ArQuantity?.ToString() ?? "0";
            upd_dp_ArDate.SelectedDate = data.ArDate;
            upd_cmb_Flag.SelectedIndex = data.ArFlag == 2 ? 1 : 0;
            upd_txt_Hidden.Text = data.ArHidden ?? string.Empty;
        }

        private void btn_CloseArrivalUpdate_Click(object sender, RoutedEventArgs e)
            => modal_ArrivalUpdate.Visibility = Visibility.Collapsed;

        private void modal_ArrivalUpdate_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => modal_ArrivalUpdate.Visibility = Visibility.Collapsed;

        private void btn_ExecuteArrivalUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchArId.Text.Trim(), out int arDetailId) || arDetailId <= 0)
            {
                ShowUpdError("更新対象の入荷明細IDを入力して情報を呼び出してください。");
                return;
            }

            if (upd_cmb_Product.SelectedValue is not int prId || prId <= 0)
            {
                ShowUpdError("商品を選択してください。");
                return;
            }

            if (!int.TryParse(upd_txt_Quantity.Text.Trim(), out int qty) || qty <= 0)
            {
                ShowUpdError("入荷数量は1以上の整数を入力してください。");
                return;
            }

            int flag = GetSelectedTag(upd_cmb_Flag);
            string? hidden = string.IsNullOrWhiteSpace(upd_txt_Hidden.Text) ? null : upd_txt_Hidden.Text.Trim();

            var result = _service.UpdateArrival(arDetailId, prId, qty, upd_dp_ArDate.SelectedDate, flag, hidden);
            if (result.Success)
            {
                ShowUpdSuccess(result.Message);
                ExecuteSearch();
            }
            else
            {
                ShowUpdError(result.Message);
            }
        }

        private void ShowUpdError(string msg)
        {
            txt_UpdErrorMessage.Text = msg;
            border_UpdErrorAlert.Visibility = Visibility.Visible;
        }

        private void ShowUpdSuccess(string msg)
        {
            txt_UpdSuccessMessage.Text = msg;
            border_UpdSuccessAlert.Visibility = Visibility.Visible;
        }

        // =====================================================================
        // 入荷非表示 モーダル
        // =====================================================================
        private void OpenHideModal(ArrivalDisplayModel data)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            hide_txt_DisplayArId.Text = data.ArId.ToString();
            hide_txt_DisplayPrName.Text = data.PrName;
            hide_txt_DisplayArDate.Text = data.ArDateFormatted;
            hide_txt_DisplayFlag.Text = data.ArFlag == 2 ? "2 (すでに非表示)" : "0 (通常表示)";
            hide_txt_Reason.Text = data.ArHidden ?? string.Empty;

            modal_ArrivalHide.Visibility = Visibility.Visible;
        }

        private void btn_CloseArrivalHide_Click(object sender, RoutedEventArgs e)
            => modal_ArrivalHide.Visibility = Visibility.Collapsed;

        private void modal_ArrivalHide_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => modal_ArrivalHide.Visibility = Visibility.Collapsed;

        private void btn_ExecuteArrivalHide_Click(object sender, RoutedEventArgs e)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(hide_txt_DisplayArId.Text.Trim(), out int arId) || arId <= 0)
            {
                ShowHideError("対象の入荷IDが取得できません。");
                return;
            }

            var confirm = MessageBox.Show(
                $"入荷ID: {arId} の管理フラグを 2 (非表示) に更新しますか？",
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            string reason = hide_txt_Reason.Text.Trim();
            var result = _service.SetArrivalHiddenFlag(arId, reason);

            if (result.Success)
            {
                ShowHideSuccess(result.Message);
                hide_txt_DisplayFlag.Text = "2 (非表示)";
                ExecuteSearch();
            }
            else
            {
                ShowHideError(result.Message);
            }
        }

        private void ShowHideError(string msg)
        {
            txt_HideErrorMessage.Text = msg;
            border_HideErrorAlert.Visibility = Visibility.Visible;
        }

        private void ShowHideSuccess(string msg)
        {
            txt_HideSuccessMessage.Text = msg;
            border_HideSuccessAlert.Visibility = Visibility.Visible;
        }

        // =====================================================================
        // ヘルパー
        // =====================================================================
        private static int GetSelectedTag(ComboBox cmb)
        {
            if (cmb.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (int.TryParse(item.Tag.ToString(), out int val)) return val;
            }
            return 0;
        }

        private void btn_DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Arrivals.SelectedItem is not ArrivalDisplayModel selected)
            {
                MessageBox.Show("削除する入荷情報を選択してください。");
                return;
            }

            var result = MessageBox.Show(
                $"次の入荷データを完全に削除しますか？\n\n" +
                $"入荷ID：{selected.ArId}\n" +
                $"商品名：{selected.PrName}\n" +
                $"数量：{selected.ArQuantityFormatted}\n\n" +
                "この操作は取り消せません。",
                "入荷データの完全削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var (success, message) = _service.DeleteArrival(selected.ArId);
            if (!success)
            {
                MessageBox.Show(message, "削除エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(message, "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
            ExecuteSearch();
        }
    }

    public class ArrivalDetailDisplayModel
    {
        public int ArDetailId { get; set; }
        public int? ArId { get; set; }
        public int? PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int? ArQuantity { get; set; }
    }
}
