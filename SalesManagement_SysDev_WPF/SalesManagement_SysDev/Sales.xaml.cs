using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class Sales : UserControl
    {
        private readonly SalesService _salesService = new();
        private List<SaleDisplayModel> _cachedList = new();
        private List<MSalesOffice> _offices = new();
        private List<MClient> _clients = new();
        private List<MEmployee> _employees = new();
        private List<MProduct> _products = new();
        private List<TOrder> _orders = new();
        private int _selectedSaleIdForHide = 0;

        public Sales()
        {
            InitializeComponent();
            Loaded += Sales_Loaded;
        }

        private void Sales_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMasters();
            ExecuteSearch();
            UpdateActionButtonsState();
        }

        private void LoadMasters()
        {
            _offices = _salesService.GetSalesOffices();
            _clients = _salesService.GetClients();
            _employees = _salesService.GetEmployees();
            _products = _salesService.GetProducts();
            _orders = _salesService.GetOrders();

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
                DisplayText = $"受注 #{o.OrId} (顧客ID: {o.ClId})"
            }).ToList();
            reg_cmb_Order.ItemsSource = orderDisplayList;
            reg_cmb_Order.DisplayMemberPath = "DisplayText";
            reg_cmb_Order.SelectedValuePath = "OrId";
            if (orderDisplayList.Count > 0) reg_cmb_Order.SelectedIndex = 0;

            UpdateEstimatedPrice();
        }

        // =====================================================================
        // 検索機能
        // =====================================================================
        private void ExecuteSearch()
        {
            int? saId = null;
            if (int.TryParse(txt_SearchSaId.Text.Trim(), out int id))
            {
                saId = id;
            }

            string clName = txt_SearchClName.Text.Trim();
            string emName = txt_SearchEmName.Text.Trim();

            int? orId = null;
            if (int.TryParse(txt_SearchOrId.Text.Trim(), out int oId))
            {
                orId = oId;
            }

            int? soId = null;
            if (cmb_SearchOffice.SelectedValue is int sId && sId > 0)
            {
                soId = sId;
            }

            DateTime? dateFrom = dp_SearchDateFrom.SelectedDate;
            DateTime? dateTo = dp_SearchDateTo.SelectedDate;
            bool includeHidden = chk_IncludeHidden.IsChecked == true;

            var results = _salesService.SearchSales(saId, clName, soId, emName, orId, dateFrom, dateTo, includeHidden);
            _cachedList = results;

            dg_Sales.ItemsSource = _cachedList;
            txt_RecordCount.Text = $"表示件数: {_cachedList.Count} 件";
            UpdateActionButtonsState();
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchSaId.Text = string.Empty;
            txt_SearchClName.Text = string.Empty;
            txt_SearchEmName.Text = string.Empty;
            txt_SearchOrId.Text = string.Empty;
            cmb_SearchOffice.SelectedIndex = 0;
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
            bool hasSelection = dg_Sales.SelectedItem is SaleDisplayModel;
            btn_DetailSelected.IsEnabled = hasSelection;
            btn_HideSelected.IsEnabled = hasSelection;
        }

        private void dg_Sales_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActionButtonsState();
        }

        private void dg_Sales_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_Sales.SelectedItem is SaleDisplayModel selected)
            {
                OpenDetailModal(selected.SaId);
            }
        }

        // =====================================================================
        // 新規登録
        // =====================================================================
        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
        {
            reg_dp_SaDate.SelectedDate = DateTime.Today;
            reg_txt_Quantity.Text = "1";

            if (_clients.Count > 0) reg_cmb_Client.SelectedIndex = 0;
            if (_offices.Count > 0) reg_cmb_Office.SelectedIndex = 0;
            if (_employees.Count > 0) reg_cmb_Employee.SelectedIndex = 0;
            if (_products.Count > 0) reg_cmb_Product.SelectedIndex = 0;
            if (reg_cmb_Order.Items.Count > 0) reg_cmb_Order.SelectedIndex = 0;

            UpdateEstimatedPrice();
            modal_Register.Visibility = Visibility.Visible;
        }

        private void btn_CancelRegister_Click(object sender, RoutedEventArgs e)
        {
            modal_Register.Visibility = Visibility.Collapsed;
        }

        private void modal_Register_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_Register.Visibility = Visibility.Collapsed;
        }

        private void reg_cmb_Product_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEstimatedPrice();
        }

        private void reg_txt_Quantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEstimatedPrice();
        }

        private void UpdateEstimatedPrice()
        {
            if (reg_txt_EstimatedPrice == null) return;

            if (reg_cmb_Product?.SelectedItem is MProduct prod && int.TryParse(reg_txt_Quantity?.Text.Trim(), out int qty) && qty > 0)
            {
                decimal total = prod.Price * qty;
                reg_txt_EstimatedPrice.Text = $"単価: {prod.Price:N0} 円 × {qty} = 合計: {total:N0} 円";
            }
            else
            {
                reg_txt_EstimatedPrice.Text = "合計金額: 0 円";
            }
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

            int emId = reg_cmb_Employee.SelectedValue is int eId ? eId : 0;
            if (emId <= 0)
            {
                MessageBox.Show("担当社員を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int orId = reg_cmb_Order.SelectedValue is int oId ? oId : 0;

            if (!int.TryParse(reg_txt_Quantity.Text.Trim(), out int quantity) || quantity <= 0)
            {
                MessageBox.Show("数量は1以上の整数を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_Quantity.Focus();
                return;
            }

            int prId = reg_cmb_Product.SelectedValue is int pId ? pId : 0;
            if (prId <= 0)
            {
                MessageBox.Show("販売商品を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var product = _products.FirstOrDefault(p => p.PrId == prId);
            decimal unitPrice = product?.Price ?? 0m;
            decimal totalPrice = unitPrice * quantity;

            var newSale = new TSale
            {
                ClId = clId,
                SoId = soId,
                EmId = emId,
                OrId = orId,
                SaDate = reg_dp_SaDate.SelectedDate ?? DateTime.Today
            };

            var details = new List<TSaleDetail>
            {
                new TSaleDetail
                {
                    PrId = prId,
                    SaQuantity = quantity,
                    SaPrTotalPrice = totalPrice
                }
            };

            var (success, message, _) = _salesService.RegisterSale(newSale, details);
            if (success)
            {
                MessageBox.Show(message, "売上登録完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Register.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(message, "売上登録失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =====================================================================
        // 詳細確認
        // =====================================================================
        private void btn_DetailSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Sales.SelectedItem is SaleDisplayModel selected)
            {
                OpenDetailModal(selected.SaId);
            }
        }

        private void OpenDetailModal(int saId)
        {
            var sale = _cachedList.FirstOrDefault(s => s.SaId == saId);
            if (sale == null) return;

            detail_txt_Subheader.Text = $"売上ID: {sale.SaId} (売上日: {sale.SaDateFormatted} / 受注ID: #{sale.OrId})";
            detail_txt_Client.Text = sale.ClName;
            detail_txt_OfficeEmployee.Text = $"{sale.SoName} ｜ {sale.EmName}";
            detail_txt_Total.Text = sale.TotalAmountFormatted;

            var details = _salesService.GetSaleDetails(saId);
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
            if (dg_Sales.SelectedItem is SaleDisplayModel selected)
            {
                _selectedSaleIdForHide = selected.SaId;
                hide_txt_TargetInfo.Text = $"売上ID: {selected.SaId} ／ 顧客: {selected.ClName} (金額: {selected.TotalAmountFormatted})";
                hide_txt_Reason.Text = "伝票取消による非表示設定";

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
            if (_selectedSaleIdForHide <= 0) return;

            string reason = hide_txt_Reason.Text.Trim();
            var (success, message) = _salesService.HideSale(_selectedSaleIdForHide, reason);

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
