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
        private readonly ChumonService _chumonService = new();
        private List<ChumonDisplayModel> _cachedList = new();
        private List<MSalesOffice> _offices = new();
        private List<MClient> _clients = new();
        private List<MEmployee> _employees = new();
        private List<MProduct> _products = new();
        private List<TOrder> _orders = new();
        private int _selectedChumonIdForHide = 0;

        public OrderManagement()
        {
            InitializeComponent();
            Loaded += OrderManagement_Loaded;
        }

        private void OrderManagement_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMasters();
            ExecuteSearch();
            UpdateActionButtonsState();
        }

        private void LoadMasters()
        {
            _offices = _chumonService.GetSalesOffices();
            _clients = _chumonService.GetClients();
            _employees = _chumonService.GetEmployees();
            _products = _chumonService.GetProducts();
            _orders = _chumonService.GetOrders();

            // 検索コンボ用 (先頭に「すべて」を追加)
            var searchOffices = new List<MSalesOffice>
            {
                new MSalesOffice { SoId = 0, SoName = "すべての営業所" }
            };
            searchOffices.AddRange(_offices);
            cmb_SearchOffice.ItemsSource = searchOffices;
            cmb_SearchOffice.SelectedIndex = 0;

            // 登録用コンボ
            reg_cmb_Office.ItemsSource = _offices;
            if (_offices.Count > 0) reg_cmb_Office.SelectedIndex = 0;

            reg_cmb_Client.ItemsSource = _clients;
            if (_clients.Count > 0) reg_cmb_Client.SelectedIndex = 0;

            reg_cmb_Employee.ItemsSource = _employees;
            if (_employees.Count > 0) reg_cmb_Employee.SelectedIndex = 0;

            reg_cmb_Product.ItemsSource = _products;
            if (_products.Count > 0) reg_cmb_Product.SelectedIndex = 0;

            var orderDisplayList = _orders.Select(o => new
            {
                OrId = o.OrId,
                ClId = o.ClId,
                SoId = o.SoId,
                EmId = o.EmId,
                DisplayText = $"受注 #{o.OrId} (顧客ID: {o.ClId})"
            }).ToList();
            reg_cmb_Order.ItemsSource = orderDisplayList;
            reg_cmb_Order.DisplayMemberPath = "DisplayText";
            reg_cmb_Order.SelectedValuePath = "OrId";
            if (orderDisplayList.Count > 0) reg_cmb_Order.SelectedIndex = 0;
        }

        // =====================================================================
        // 検索機能
        // =====================================================================
        private void ExecuteSearch()
        {
            int? chId = null;
            if (int.TryParse(txt_SearchChId.Text.Trim(), out int cId))
            {
                chId = cId;
            }

            int? orId = null;
            if (int.TryParse(txt_SearchOrId.Text.Trim(), out int oId))
            {
                orId = oId;
            }

            string clName = txt_SearchClName.Text.Trim();
            string emName = txt_SearchEmName.Text.Trim();

            int? soId = null;
            if (cmb_SearchOffice.SelectedValue is int sId && sId > 0)
            {
                soId = sId;
            }

            int? stateFlag = null;
            if (cmb_SearchState.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int state) && state >= 0)
            {
                stateFlag = state;
            }

            DateTime? dateFrom = dp_SearchDateFrom.SelectedDate;
            DateTime? dateTo = dp_SearchDateTo.SelectedDate;
            bool includeHidden = chk_IncludeHidden.IsChecked == true;

            var results = _chumonService.SearchChumons(chId, clName, soId, emName, orId, dateFrom, dateTo, stateFlag, includeHidden);
            _cachedList = results;

            dg_Chumons.ItemsSource = _cachedList;
            txt_RecordCount.Text = $"表示件数: {_cachedList.Count} 件";
            UpdateActionButtonsState();
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchChId.Text = string.Empty;
            txt_SearchOrId.Text = string.Empty;
            txt_SearchClName.Text = string.Empty;
            txt_SearchEmName.Text = string.Empty;
            cmb_SearchOffice.SelectedIndex = 0;
            cmb_SearchState.SelectedIndex = 0;
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

        private void chk_IncludeHidden_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void cmb_Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ExecuteSearch();
            }
        }

        private void dp_Filter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ExecuteSearch();
            }
        }

        private void UpdateActionButtonsState()
        {
            bool hasSelection = dg_Chumons.SelectedItem is ChumonDisplayModel;
            btn_DetailSelected.IsEnabled = hasSelection;
            btn_HideSelected.IsEnabled = hasSelection;
        }

        private void dg_Chumons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActionButtonsState();
        }

        private void dg_Chumons_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_Chumons.SelectedItem is ChumonDisplayModel selected)
            {
                OpenDetailModal(selected.ChId);
            }
        }

        // =====================================================================
        // 新規登録
        // =====================================================================
        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
        {
            reg_dp_ChDate.SelectedDate = DateTime.Today;
            reg_txt_Quantity.Text = "1";

            if (reg_cmb_Order.Items.Count > 0) reg_cmb_Order.SelectedIndex = 0;
            if (_clients.Count > 0) reg_cmb_Client.SelectedIndex = 0;
            if (_offices.Count > 0) reg_cmb_Office.SelectedIndex = 0;
            if (_employees.Count > 0) reg_cmb_Employee.SelectedIndex = 0;
            if (_products.Count > 0) reg_cmb_Product.SelectedIndex = 0;

            modal_Register.Visibility = Visibility.Visible;
        }

        private void reg_cmb_Order_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reg_cmb_Order.SelectedValue is int orId && orId > 0)
            {
                var ord = _orders.FirstOrDefault(o => o.OrId == orId);
                if (ord != null)
                {
                    reg_cmb_Client.SelectedValue = ord.ClId;
                    reg_cmb_Office.SelectedValue = ord.SoId;
                    reg_cmb_Employee.SelectedValue = ord.EmId;
                }
            }
        }

        private void btn_CancelRegister_Click(object sender, RoutedEventArgs e)
        {
            modal_Register.Visibility = Visibility.Collapsed;
        }

        private void modal_Register_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_Register.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteRegister_Click(object sender, RoutedEventArgs e)
        {
            int clId = reg_cmb_Client.SelectedValue is int cId ? cId : 0;
            if (clId <= 0)
            {
                MessageBox.Show("顧客を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int soId = reg_cmb_Office.SelectedValue is int sId ? sId : 0;
            if (soId <= 0)
            {
                MessageBox.Show("営業所を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int orId = reg_cmb_Order.SelectedValue is int oId ? oId : 0;
            if (orId <= 0)
            {
                MessageBox.Show("関連受注を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? emId = reg_cmb_Employee.SelectedValue is int eId && eId > 0 ? eId : null;

            if (!int.TryParse(reg_txt_Quantity.Text.Trim(), out int quantity) || quantity <= 0)
            {
                MessageBox.Show("数量は1以上の整数を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_Quantity.Focus();
                return;
            }

            int prId = reg_cmb_Product.SelectedValue is int pId ? pId : 0;
            if (prId <= 0)
            {
                MessageBox.Show("商品を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newChumon = new TChumon
            {
                OrId = orId,
                ClId = clId,
                SoId = soId,
                EmId = emId,
                ChDate = reg_dp_ChDate.SelectedDate ?? DateTime.Today
            };

            var details = new List<TChumonDetail>
            {
                new TChumonDetail
                {
                    PrId = prId,
                    ChQuantity = quantity
                }
            };

            var (success, message, _) = _chumonService.RegisterChumon(newChumon, details);
            if (success)
            {
                MessageBox.Show(message, "注文登録完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Register.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(message, "注文登録失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =====================================================================
        // 詳細確認
        // =====================================================================
        private void btn_DetailSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Chumons.SelectedItem is ChumonDisplayModel selected)
            {
                OpenDetailModal(selected.ChId);
            }
        }

        private void OpenDetailModal(int chId)
        {
            var chumon = _cachedList.FirstOrDefault(c => c.ChId == chId);
            if (chumon == null) return;

            detail_txt_Subheader.Text = $"注文ID: {chumon.ChId} (注文日: {chumon.ChDateFormatted})";
            detail_txt_Client.Text = chumon.ClName;
            detail_txt_OfficeEmployee.Text = $"{chumon.SoName} ｜ {chumon.EmName}";
            detail_txt_OrderState.Text = $"受注 #{chumon.OrId} ｜ {chumon.StateFormatted}";

            var details = _chumonService.GetChumonDetails(chId);
            dg_DetailItems.ItemsSource = details;

            modal_Detail.Visibility = Visibility.Visible;
        }

        private void btn_CloseDetail_Click(object sender, RoutedEventArgs e)
        {
            modal_Detail.Visibility = Visibility.Collapsed;
        }

        private void modal_Detail_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_Detail.Visibility = Visibility.Collapsed;
        }

        // =====================================================================
        // 非表示 (論理削除)
        // =====================================================================
        private void btn_HideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Chumons.SelectedItem is ChumonDisplayModel selected)
            {
                _selectedChumonIdForHide = selected.ChId;
                hide_txt_TargetInfo.Text = $"注文ID: {selected.ChId} ／ 受注 #{selected.OrId}（顧客: {selected.ClName}）";
                hide_txt_Reason.Text = "注文取消による非表示設定";

                modal_Hide.Visibility = Visibility.Visible;
                hide_txt_Reason.Focus();
            }
        }

        private void btn_CancelHide_Click(object sender, RoutedEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        private void modal_Hide_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteHide_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedChumonIdForHide <= 0) return;

            string reason = hide_txt_Reason.Text.Trim();
            var (success, message) = _chumonService.HideChumon(_selectedChumonIdForHide, reason);

            if (success)
            {
                MessageBox.Show(message, "非表示設定完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Hide.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(message, "処理失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
