using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// Shipment.xaml の相互作用ロジック (14. 出荷管理)
    /// </summary>
    public partial class Shipment : UserControl
    {
        private readonly ShipmentService _shipmentService = new ShipmentService();
        private List<ShipmentDisplayModel> _currentList = new List<ShipmentDisplayModel>();
        private List<TOrder> _cachedOrders = new List<TOrder>();

        public Shipment()
        {
            InitializeComponent();
            Loaded += Shipment_Loaded;
        }

        private void Shipment_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMasterData();
            ExecuteSearch();
        }

        // =====================================================================
        // マスタデータ読み込み
        // =====================================================================
        private void LoadMasterData()
        {
            try
            {
                var offices = _shipmentService.GetSalesOffices();
                var searchOffices = new List<MSalesOffice>
                {
                    new MSalesOffice { SoId = 0, SoName = "すべての営業所" }
                };
                searchOffices.AddRange(offices);
                cmb_SearchOffice.ItemsSource = searchOffices;
                cmb_SearchOffice.SelectedIndex = 0;

                var employees = _shipmentService.GetEmployees();
                reg_cmb_Employee.ItemsSource = employees;
                if (employees.Count > 0)
                {
                    reg_cmb_Employee.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"マスタデータの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================================
        // 検索実行
        // =====================================================================
        private void ExecuteSearch()
        {
            int? shId = null;
            if (int.TryParse(txt_SearchShId.Text.Trim(), out int parsedShId) && parsedShId > 0)
            {
                shId = parsedShId;
            }

            int? orId = null;
            if (int.TryParse(txt_SearchOrId.Text.Trim(), out int parsedOrId) && parsedOrId > 0)
            {
                orId = parsedOrId;
            }

            string? clName = string.IsNullOrWhiteSpace(txt_SearchClName.Text) ? null : txt_SearchClName.Text.Trim();

            int? soId = null;
            if (cmb_SearchOffice.SelectedValue is int selectedSoId && selectedSoId > 0)
            {
                soId = selectedSoId;
            }

            string? emName = string.IsNullOrWhiteSpace(txt_SearchEmName.Text) ? null : txt_SearchEmName.Text.Trim();

            int? stateFlag = null;
            if (cmb_SearchState.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (int.TryParse(item.Tag.ToString(), out int sFlag))
                {
                    stateFlag = sFlag;
                }
            }

            DateTime? dateFrom = dp_SearchDateFrom.SelectedDate;
            DateTime? dateTo = dp_SearchDateTo.SelectedDate;
            bool includeHidden = chk_IncludeHidden.IsChecked == true;

            _currentList = _shipmentService.SearchShipments(
                shId: shId,
                orId: orId,
                clName: clName,
                soId: soId,
                emName: emName,
                dateFrom: dateFrom,
                dateTo: dateTo,
                stateFlag: stateFlag,
                includeHidden: includeHidden);

            dg_Shipments.ItemsSource = _currentList;
            txt_RecordCount.Text = $"表示件数: {_currentList.Count} 件";
            UpdateToolbarButtons();
        }

        private void UpdateToolbarButtons()
        {
            bool hasSelection = dg_Shipments.SelectedItem is ShipmentDisplayModel;
            btn_OpenDetail.IsEnabled = hasSelection;
            btn_OpenHide.IsEnabled = hasSelection;
            btn_OpenDelete.IsEnabled = hasSelection;
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e) => ExecuteSearch();

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchShId.Text = string.Empty;
            txt_SearchOrId.Text = string.Empty;
            txt_SearchClName.Text = string.Empty;
            if (cmb_SearchOffice.Items.Count > 0) cmb_SearchOffice.SelectedIndex = 0;
            txt_SearchEmName.Text = string.Empty;
            if (cmb_SearchState.Items.Count > 0) cmb_SearchState.SelectedIndex = 0;
            dp_SearchDateFrom.SelectedDate = null;
            dp_SearchDateTo.SelectedDate = null;
            chk_IncludeHidden.IsChecked = false;
            ExecuteSearch();
        }

        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteSearch();
            }
        }

        private void cmb_Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ExecuteSearch();
        }

        private void dp_Filter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ExecuteSearch();
        }

        private void chk_IncludeHidden_Click(object sender, RoutedEventArgs e) => ExecuteSearch();

        private void dg_Shipments_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateToolbarButtons();

        private void dg_Shipments_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_Shipments.SelectedItem is ShipmentDisplayModel)
            {
                OpenDetailModal();
            }
        }

        // =====================================================================
        // モーダル 1: 出荷登録
        // =====================================================================
        private void btn_OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cachedOrders = _shipmentService.GetOrders();
                var orderOptions = _cachedOrders.Select(o =>
                {
                    bool isSyukkoDone = o.TSyukkos.Any(s => s.SyFlag == 0 && s.SyStateFlag == 1);
                    bool hasSyukko = o.TSyukkos.Any(s => s.SyFlag == 0);
                    bool isArrivalDone = o.TArrivals.Any(a => a.ArFlag == 0 && a.ArStateFlag == 1);
                    bool hasShipment = o.TShipments.Any(s => s.ShFlag == 0);

                    string statusBadge = hasShipment ? "[出荷登録済]" :
                                         (isSyukkoDone || isArrivalDone) ? "[出庫/検品完了・出荷可]" :
                                         hasSyukko ? "[出庫準備中]" : "[出庫未手配]";

                    return new
                    {
                        OrId = o.OrId,
                        DisplayText = $"受注ID: {o.OrId} / {o.Cl?.ClName} / {statusBadge}（品目: {o.TOrderDetails?.Count ?? 0}件）",
                        Priority = hasShipment ? 2 : (isSyukkoDone || isArrivalDone ? 0 : 1)
                    };
                })
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.OrId)
                .ToList();

                reg_cmb_Order.ItemsSource = orderOptions;
                reg_cmb_Order.DisplayMemberPath = "DisplayText";
                reg_cmb_Order.SelectedValuePath = "OrId";

                if (orderOptions.Count > 0)
                {
                    reg_cmb_Order.SelectedIndex = 0;
                }
                else
                {
                    reg_txt_Client.Text = "-";
                    reg_txt_Office.Text = "-";
                    reg_dg_Details.ItemsSource = null;
                }

                reg_dp_FinishDate.SelectedDate = DateTime.Today;
                modal_Register.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"登録画面の初期化に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void reg_cmb_Order_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reg_cmb_Order.SelectedValue is int orId && orId > 0)
            {
                var target = _cachedOrders.FirstOrDefault(o => o.OrId == orId);
                if (target != null)
                {
                    reg_txt_Client.Text = target.Cl?.ClName ?? $"顧客ID: {target.ClId}";
                    reg_txt_Office.Text = target.So?.SoName ?? $"営業所ID: {target.SoId}";

                    var details = target.TOrderDetails?.Select(d => new
                    {
                        PrId = d.PrId,
                        PrName = d.Pr?.PrName ?? $"商品ID: {d.PrId}",
                        ShQuantity = d.OrQuantity
                    }).ToList();

                    reg_dg_Details.ItemsSource = details;
                    return;
                }
            }

            reg_txt_Client.Text = string.Empty;
            reg_txt_Office.Text = string.Empty;
            reg_dg_Details.ItemsSource = null;
        }

        private void btn_CancelRegister_Click(object sender, RoutedEventArgs e) => modal_Register.Visibility = Visibility.Collapsed;

        private void modal_Register_Backdrop_Click(object sender, MouseButtonEventArgs e) => modal_Register.Visibility = Visibility.Collapsed;

        private void btn_ExecuteRegister_Click(object sender, RoutedEventArgs e)
        {
            if (reg_cmb_Order.SelectedValue is not int orId || orId <= 0)
            {
                MessageBox.Show("対象の受注を選択してください。", "入力チェック", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var target = _cachedOrders.FirstOrDefault(o => o.OrId == orId);
            if (target == null)
            {
                MessageBox.Show("受注情報の取得に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int? emId = null;
            if (reg_cmb_Employee.SelectedValue is int parsedEmId && parsedEmId > 0)
            {
                emId = parsedEmId;
            }

            var newShipment = new TShipment
            {
                OrId = target.OrId,
                ClId = target.ClId,
                SoId = target.SoId,
                EmId = emId,
                ShFinishDate = reg_dp_FinishDate.SelectedDate ?? DateTime.Today,
                ShStateFlag = 0
            };

            var details = new List<TShipmentDetail>();
            if (target.TOrderDetails != null)
            {
                foreach (var od in target.TOrderDetails)
                {
                    details.Add(new TShipmentDetail
                    {
                        PrId = od.PrId,
                        ShQuantity = od.OrQuantity
                    });
                }
            }

            if (details.Count == 0)
            {
                MessageBox.Show("選択した受注に商品明細が含まれていません。", "入力チェック", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (success, msg, _) = _shipmentService.RegisterShipment(newShipment, details);
            if (success)
            {
                MessageBox.Show(msg, "登録完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Register.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(msg, "登録エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================================
        // モーダル 2: 出荷詳細・確定
        // =====================================================================
        private void btn_OpenDetail_Click(object sender, RoutedEventArgs e) => OpenDetailModal();

        private void OpenDetailModal()
        {
            if (dg_Shipments.SelectedItem is not ShipmentDisplayModel selected) return;

            detail_txt_HeaderTitle.Text = $"出荷詳細 (出荷ID: {selected.ShId})";
            detail_txt_ShId.Text = $"出荷ID: {selected.ShId} (受注ID: {selected.OrId})";
            detail_txt_Client.Text = $"顧客: {selected.ClName}";
            detail_txt_Office.Text = $"営業所: {selected.SoName} / 担当: {selected.EmName}";
            detail_txt_State.Text = $"状態: {selected.StateFormatted} / 完了日: {selected.ShFinishDateFormatted}";

            modal_Detail.Tag = selected.ShId;

            // 確定ボタンの活性・非活性制御
            btn_ConfirmShipment.IsEnabled = (selected.ShStateFlag != 1);
            btn_ConfirmShipment.Content = (selected.ShStateFlag == 1) ? "✔ 確定済み（出荷・在庫減算完了）" : "✔ 出荷を確定する（在庫減算）";

            var details = _shipmentService.GetShipmentDetails(selected.ShId);
            detail_dg_Items.ItemsSource = details;

            modal_Detail.Visibility = Visibility.Visible;
        }

        private void btn_CloseDetail_Click(object sender, RoutedEventArgs e) => modal_Detail.Visibility = Visibility.Collapsed;

        private void modal_Detail_Backdrop_Click(object sender, MouseButtonEventArgs e) => modal_Detail.Visibility = Visibility.Collapsed;

        private void btn_ConfirmShipment_Click(object sender, RoutedEventArgs e)
        {
            if (modal_Detail.Tag is not int shId || shId <= 0) return;

            var confirm = MessageBox.Show(
                $"出荷ID: {shId} を確定しますか？\n出荷を確定すると、在庫テーブルから出荷数量分の在庫が減算されます。",
                "出荷確定確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var (success, msg) = _shipmentService.UpdateShipment(
                shId: shId,
                emId: null,
                finishDate: DateTime.Today,
                stateFlag: 1);

            if (success)
            {
                MessageBox.Show("出荷を確定しました。在庫数が減算されました。", "確定完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Detail.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(msg, "確定エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================================
        // モーダル 3: 出荷非表示設定 (論理削除)
        // =====================================================================
        private void btn_OpenHide_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Shipments.SelectedItem is not ShipmentDisplayModel selected) return;

            hide_txt_TargetInfo.Text = $"出荷ID: {selected.ShId} / 受注ID: {selected.OrId} / 顧客: {selected.ClName}";
            hide_txt_Reason.Text = string.IsNullOrWhiteSpace(selected.ShHidden) ? "出荷取消による非表示設定" : selected.ShHidden;
            modal_Hide.Tag = selected.ShId;

            modal_Hide.Visibility = Visibility.Visible;
        }

        private void btn_OpenDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Shipments.SelectedItem is not ShipmentDisplayModel selected)
            {
                MessageBox.Show("削除する出荷を選択してください。");
                return;
            }

            var result = MessageBox.Show(
                $"次の出荷データを完全に削除しますか？\n\n" +
                $"出荷ID：{selected.ShId}\n" +
                $"受注ID：{selected.OrId}\n" +
                $"顧客名：{selected.ClName}\n\n" +
                "この操作は取り消せません。",
                "出荷の完全削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var (success, message) = _shipmentService.DeleteShipment(selected.ShId);

            if (!success)
            {
                MessageBox.Show(message, "削除できません",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(message, "削除完了",
                MessageBoxButton.OK, MessageBoxImage.Information);

            ExecuteSearch();
        }

        private void btn_CancelHide_Click(object sender, RoutedEventArgs e) => modal_Hide.Visibility = Visibility.Collapsed;

        private void modal_Hide_Backdrop_Click(object sender, MouseButtonEventArgs e) => modal_Hide.Visibility = Visibility.Collapsed;

        private void btn_ExecuteHide_Click(object sender, RoutedEventArgs e)
        {
            if (modal_Hide.Tag is not int shId || shId <= 0) return;

            string reason = hide_txt_Reason.Text.Trim();
            var (success, msg) = _shipmentService.HideShipment(shId, reason);

            if (success)
            {
                MessageBox.Show(msg, "非表示完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Hide.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(msg, "非表示エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
