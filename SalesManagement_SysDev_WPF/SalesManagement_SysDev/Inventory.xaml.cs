using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class Inventory : UserControl
    {
        private readonly StockService _stockService = new();
        private readonly ProductService _productService = new();
        private List<StockDisplayModel> _cachedStockList = new();

        public Inventory()
        {
            InitializeComponent();
        }

        // =====================================================================
        // カードクリック イベントハンドラ
        // =====================================================================
        private void card_InventoryRegister_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenInventoryRegisterModal();
        }

        private void card_InventoryUpdate_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenInventoryUpdateModal();
        }

        private void card_InventoryList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenInventoryListModal();
        }

        private void card_InventorySearch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenInventorySearchModal();
        }

        private void card_InventoryHide_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenInventoryHideModal();
        }

        // =====================================================================
        // モーダル: 在庫登録
        // =====================================================================
        public void OpenInventoryRegisterModal()
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            var products = _productService.GetAllProducts(includeHidden: false);
            reg_cmb_Product.ItemsSource = products;
            if (products.Count > 0)
            {
                reg_cmb_Product.SelectedIndex = 0;
            }
            reg_txt_Quantity.Text = "0";

            modal_InventoryRegister.Visibility = Visibility.Visible;
        }

        private void reg_cmb_Product_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reg_cmb_Product.SelectedItem is ProductDisplayModel p)
            {
                reg_txt_ProductInfo.Text = $"{p.MaName} / {p.ScName}";
                reg_txt_SafetyStock.Text = $"{p.PrSafetyStock} 個";
            }
            else
            {
                reg_txt_ProductInfo.Text = "-";
                reg_txt_SafetyStock.Text = "-";
            }
        }

        private void btn_CloseInventoryRegister_Click(object sender, RoutedEventArgs e)
        {
            modal_InventoryRegister.Visibility = Visibility.Collapsed;
        }

        private void modal_InventoryRegister_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_InventoryRegister.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteInventoryRegister_Click(object sender, RoutedEventArgs e)
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            if (reg_cmb_Product.SelectedValue is not int prId || prId <= 0)
            {
                ShowRegError("対象商品を選択してください。");
                return;
            }

            if (!int.TryParse(reg_txt_Quantity.Text.Trim(), out int qty) || qty < 0)
            {
                ShowRegError("在庫数は0以上の半角整数を入力してください。");
                return;
            }

            var result = _stockService.RegisterStock(prId, qty);
            if (result.Success)
            {
                ShowRegSuccess(result.Message);
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
        // 5.1 モーダル: 在庫更新
        // =====================================================================
        public void OpenInventoryUpdateModal(int? targetStId = null)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            ClearUpdateFields();
            upd_txt_SearchStId.Text = targetStId.HasValue ? targetStId.Value.ToString() : "";
            modal_InventoryUpdate.Visibility = Visibility.Visible;

            if (targetStId.HasValue && targetStId.Value > 0)
            {
                LoadStockForUpdate(targetStId.Value);
            }
            else
            {
                upd_txt_SearchStId.Focus();
            }
        }

        private void ClearUpdateFields()
        {
            upd_txt_DisplayPrName.Text = "-";
            upd_txt_DisplayMaker.Text = "-";
            upd_txt_DisplaySafetyStock.Text = "-";
            upd_txt_DisplayCurrentQty.Text = "-";
            upd_txt_Quantity.Text = "";
            upd_cmb_StFlag.SelectedIndex = 0;
        }

        private void btn_CloseInventoryUpdate_Click(object sender, RoutedEventArgs e)
        {
            modal_InventoryUpdate.Visibility = Visibility.Collapsed;
        }

        private void modal_InventoryUpdate_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_InventoryUpdate.Visibility = Visibility.Collapsed;
        }

        private void upd_txt_SearchStId_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_LoadStockForUpdate_Click(sender, e);
            }
        }

        private void btn_LoadStockForUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchStId.Text.Trim(), out int stId) || stId <= 0)
            {
                ShowUpdError("有効な在庫IDを半角数値で入力してください。");
                return;
            }

            LoadStockForUpdate(stId);
        }

        private void LoadStockForUpdate(int stId)
        {
            var stock = _stockService.GetStockById(stId);
            if (stock == null)
            {
                ShowUpdError($"在庫ID: {stId} のデータは見つかりませんでした。");
                ClearUpdateFields();
                return;
            }

            upd_txt_SearchStId.Text = stock.StId.ToString();
            upd_txt_DisplayPrName.Text = stock.Pr?.PrName ?? $"商品ID: {stock.PrId}";
            upd_txt_DisplayMaker.Text = $"{stock.Pr?.Ma?.MaName ?? "-"} / {stock.Pr?.Sc?.ScName ?? "-"}";
            upd_txt_DisplaySafetyStock.Text = $"{stock.Pr?.PrSafetyStock ?? 0} 個";
            upd_txt_DisplayCurrentQty.Text = $"{stock.StQuantity:N0} 個";
            upd_txt_Quantity.Text = stock.StQuantity.ToString();
            upd_cmb_StFlag.SelectedIndex = stock.StFlag == 2 ? 1 : 0;
        }

        private void btn_ExecuteInventoryUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchStId.Text.Trim(), out int stId) || stId <= 0)
            {
                ShowUpdError("対象の在庫IDを入力して情報を呼び出してください。");
                return;
            }

            if (!int.TryParse(upd_txt_Quantity.Text.Trim(), out int qty) || qty < 0)
            {
                ShowUpdError("更新後の在庫数は0以上の半角整数を入力してください。");
                return;
            }

            int stFlag = upd_cmb_StFlag.SelectedIndex == 1 ? 2 : 0;
            var result = _stockService.UpdateStock(stId, qty, stFlag);
            if (result.Success)
            {
                ShowUpdSuccess(result.Message);
                upd_txt_DisplayCurrentQty.Text = $"{qty:N0} 個";
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
        // 5.2 モーダル: 在庫一覧表示
        // =====================================================================
        public void OpenInventoryListModal()
        {
            txt_ListFilterKeyword.Text = "";
            chk_ListIncludeHidden.IsChecked = false;
            modal_InventoryList.Visibility = Visibility.Visible;
            LoadInventoryList();
        }

        private void LoadInventoryList()
        {
            bool includeHidden = chk_ListIncludeHidden.IsChecked == true;
            _cachedStockList = _stockService.GetAllStocks(includeHidden);
            ApplyInventoryListFilter();
        }

        private void ApplyInventoryListFilter()
        {
            string keyword = (txt_ListFilterKeyword.Text ?? "").Trim();
            var filtered = _cachedStockList;

            if (!string.IsNullOrEmpty(keyword))
            {
                filtered = filtered.Where(s =>
                    s.PrName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    s.StId.ToString().Contains(keyword) ||
                    s.PrId.ToString().Contains(keyword) ||
                    s.MaName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    s.ScName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    s.PrModelNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            dg_InventoryList.ItemsSource = filtered;
            txt_ListCount.Text = $"表示件数: {filtered.Count} 件 (全 {_cachedStockList.Count} 件中)";
        }

        private void txt_ListFilterKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            ApplyInventoryListFilter();
        }

        private void chk_ListIncludeHidden_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryList();
        }

        private void btn_ListRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryList();
        }

        private void btn_CloseInventoryList_Click(object sender, RoutedEventArgs e)
        {
            modal_InventoryList.Visibility = Visibility.Collapsed;
        }

        private void modal_InventoryList_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_InventoryList.Visibility = Visibility.Collapsed;
        }

        private void dg_InventoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_InventoryList.SelectedItem is StockDisplayModel selected)
            {
                modal_InventoryList.Visibility = Visibility.Collapsed;
                OpenInventoryUpdateModal(selected.StId);
            }
        }

        private void btn_ListEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_InventoryList.SelectedItem is StockDisplayModel selected)
            {
                modal_InventoryList.Visibility = Visibility.Collapsed;
                OpenInventoryUpdateModal(selected.StId);
            }
            else
            {
                MessageBox.Show("一覧から更新する在庫を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btn_ListHideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_InventoryList.SelectedItem is StockDisplayModel selected)
            {
                modal_InventoryList.Visibility = Visibility.Collapsed;
                OpenInventoryHideModal(selected.StId);
            }
            else
            {
                MessageBox.Show("一覧から非表示にする在庫を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =====================================================================
        // 5.3 モーダル: 在庫検索
        // =====================================================================
        public void OpenInventorySearchModal()
        {
            var makers = _productService.GetMakers();
            var makerOptions = new List<MMaker> { new MMaker { MaId = 0, MaName = "すべてのメーカー" } };
            makerOptions.AddRange(makers);
            srch_cmb_Maker.ItemsSource = makerOptions;
            srch_cmb_Maker.SelectedIndex = 0;

            var smallClasses = _productService.GetSmallClassifications();
            var scOptions = new List<MSmallClassification> { new MSmallClassification { ScId = 0, ScName = "すべての小分類" } };
            scOptions.AddRange(smallClasses);
            srch_cmb_SmallClass.ItemsSource = scOptions;
            srch_cmb_SmallClass.SelectedIndex = 0;

            srch_txt_StId.Text = "";
            srch_txt_PrId.Text = "";
            srch_txt_PrName.Text = "";
            srch_txt_MinQty.Text = "";
            srch_txt_MaxQty.Text = "";

            modal_InventorySearch.Visibility = Visibility.Visible;
            ExecuteInventorySearch();
        }

        private void btn_CloseInventorySearch_Click(object sender, RoutedEventArgs e)
        {
            modal_InventorySearch.Visibility = Visibility.Collapsed;
        }

        private void modal_InventorySearch_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_InventorySearch.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteInventorySearch_Click(object sender, RoutedEventArgs e)
        {
            ExecuteInventorySearch();
        }

        private void ExecuteInventorySearch()
        {
            int? stId = null;
            if (int.TryParse(srch_txt_StId.Text.Trim(), out int parsedStId) && parsedStId > 0)
            {
                stId = parsedStId;
            }

            int? prId = null;
            if (int.TryParse(srch_txt_PrId.Text.Trim(), out int parsedPrId) && parsedPrId > 0)
            {
                prId = parsedPrId;
            }

            string? name = string.IsNullOrWhiteSpace(srch_txt_PrName.Text) ? null : srch_txt_PrName.Text.Trim();

            int? maId = null;
            if (srch_cmb_Maker.SelectedValue is int selectedMa && selectedMa > 0)
            {
                maId = selectedMa;
            }

            int? scId = null;
            if (srch_cmb_SmallClass.SelectedValue is int selectedSc && selectedSc > 0)
            {
                scId = selectedSc;
            }

            int? minQty = null;
            if (int.TryParse(srch_txt_MinQty.Text.Trim(), out int qMin))
            {
                minQty = qMin;
            }

            int? maxQty = null;
            if (int.TryParse(srch_txt_MaxQty.Text.Trim(), out int qMax))
            {
                maxQty = qMax;
            }

            var results = _stockService.SearchStocks(
                stId: stId,
                prId: prId,
                prName: name,
                maId: maId,
                scId: scId,
                minQty: minQty,
                maxQty: maxQty,
                includeHidden: false);

            dg_InventorySearchResults.ItemsSource = results;
            srch_txt_Count.Text = $"抽出件数: {results.Count} 件";
        }

        private void btn_ClearInventorySearch_Click(object sender, RoutedEventArgs e)
        {
            srch_txt_StId.Text = "";
            srch_txt_PrId.Text = "";
            srch_txt_PrName.Text = "";
            srch_cmb_Maker.SelectedIndex = 0;
            srch_cmb_SmallClass.SelectedIndex = 0;
            srch_txt_MinQty.Text = "";
            srch_txt_MaxQty.Text = "";
            ExecuteInventorySearch();
        }

        private void dg_InventorySearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_InventorySearchResults.SelectedItem is StockDisplayModel selected)
            {
                modal_InventorySearch.Visibility = Visibility.Collapsed;
                OpenInventoryUpdateModal(selected.StId);
            }
        }

        private void btn_SearchEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_InventorySearchResults.SelectedItem is StockDisplayModel selected)
            {
                modal_InventorySearch.Visibility = Visibility.Collapsed;
                OpenInventoryUpdateModal(selected.StId);
            }
            else
            {
                MessageBox.Show("一覧から更新する在庫を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =====================================================================
        // 5.4 モーダル: 在庫非表示 (フラグ 0 -> 2 更新)
        // =====================================================================
        public void OpenInventoryHideModal(int? targetStId = null)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            hide_txt_StId.Text = targetStId.HasValue ? targetStId.Value.ToString() : "";
            hide_txt_DisplayPrName.Text = "-";
            hide_txt_DisplayMaker.Text = "-";
            hide_txt_DisplayQty.Text = "-";
            hide_txt_DisplayFlag.Text = "-";

            modal_InventoryHide.Visibility = Visibility.Visible;

            if (targetStId.HasValue && targetStId.Value > 0)
            {
                LoadStockForHide(targetStId.Value);
            }
            else
            {
                hide_txt_StId.Focus();
            }
        }

        private void btn_CloseInventoryHide_Click(object sender, RoutedEventArgs e)
        {
            modal_InventoryHide.Visibility = Visibility.Collapsed;
        }

        private void modal_InventoryHide_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_InventoryHide.Visibility = Visibility.Collapsed;
        }

        private void hide_txt_StId_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_LoadStockForHide_Click(sender, e);
            }
        }

        private void btn_LoadStockForHide_Click(object sender, RoutedEventArgs e)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(hide_txt_StId.Text.Trim(), out int stId) || stId <= 0)
            {
                ShowHideError("有効な在庫IDを入力してください。");
                return;
            }

            LoadStockForHide(stId);
        }

        private void LoadStockForHide(int stId)
        {
            var stock = _stockService.GetStockById(stId);
            if (stock == null)
            {
                ShowHideError($"在庫ID: {stId} のデータが見つかりません。");
                hide_txt_DisplayPrName.Text = "-";
                hide_txt_DisplayMaker.Text = "-";
                hide_txt_DisplayQty.Text = "-";
                hide_txt_DisplayFlag.Text = "-";
                return;
            }

            hide_txt_StId.Text = stock.StId.ToString();
            hide_txt_DisplayPrName.Text = stock.Pr?.PrName ?? $"商品ID: {stock.PrId}";
            hide_txt_DisplayMaker.Text = $"{stock.Pr?.Ma?.MaName ?? "-"} / {stock.Pr?.Sc?.ScName ?? "-"}";
            hide_txt_DisplayQty.Text = $"{stock.StQuantity:N0} 個";
            hide_txt_DisplayFlag.Text = stock.StFlag == 2 ? "2 (すでに非表示)" : "0 (通常表示中)";
        }

        private void btn_ExecuteInventoryHide_Click(object sender, RoutedEventArgs e)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(hide_txt_StId.Text.Trim(), out int stId) || stId <= 0)
            {
                ShowHideError("対象の在庫IDを入力して情報を呼び出してください。");
                return;
            }

            var confirm = MessageBox.Show(
                $"在庫ID: {stId}（{hide_txt_DisplayPrName.Text}）の在庫管理フラグを 2 (非表示) に更新しますか？\n（更新後は通常の一覧や検索に表示されなくなります）",
                "在庫非表示更新の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var result = _stockService.SetStockHiddenFlag(stId);
            if (result.Success)
            {
                ShowHideSuccess(result.Message);
                hide_txt_DisplayFlag.Text = "2 (非表示)";
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
    }
}
