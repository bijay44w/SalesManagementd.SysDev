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
            Loaded += Inventory_Loaded;
        }

        private void Inventory_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMainMasters();
            ExecuteMainSearch();
            UpdateMainActionButtonsState();
        }

        private void LoadMainMasters()
        {
            var makers = _productService.GetMakers();
            var searchMakers = new List<MMaker> { new MMaker { MaId = 0, MaName = "すべてのメーカー" } };
            searchMakers.AddRange(makers);
            cmb_MainMaker.ItemsSource = searchMakers;
            cmb_MainMaker.SelectedIndex = 0;
        }

        private void ExecuteMainSearch()
        {
            int? stId = null;
            if (int.TryParse(txt_MainStId.Text.Trim(), out int id)) stId = id;

            string prName = txt_MainPrName.Text.Trim();

            int? maId = null;
            if (cmb_MainMaker.SelectedValue is int mId && mId > 0) maId = mId;

            bool includeHidden = chk_MainIncludeHidden.IsChecked == true;

            var results = _stockService.SearchStocks(stId: stId, prId: null, prName: prName, maId: maId, scId: null, minQty: null, maxQty: null, includeHidden: includeHidden);

            if (chk_MainBelowSafety.IsChecked == true)
            {
                results = results.Where(s => s.StQuantity < s.PrSafetyStock).ToList();
            }

            _cachedStockList = results;
            dg_MainStocks.ItemsSource = _cachedStockList;
            txt_MainStockCount.Text = $"表示件数: {_cachedStockList.Count} 件";
            UpdateMainActionButtonsState();
        }

        private void btn_MainSearch_Click(object sender, RoutedEventArgs e)
        {
            ExecuteMainSearch();
        }

        private void btn_MainClear_Click(object sender, RoutedEventArgs e)
        {
            txt_MainStId.Text = string.Empty;
            txt_MainPrName.Text = string.Empty;
            cmb_MainMaker.SelectedIndex = 0;
            chk_MainBelowSafety.IsChecked = false;
            chk_MainIncludeHidden.IsChecked = false;
            ExecuteMainSearch();
        }

        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteMainSearch();
            }
        }

        private void chk_Filter_Click(object sender, RoutedEventArgs e)
        {
            ExecuteMainSearch();
        }

        private void UpdateMainActionButtonsState()
        {
            bool hasSelection = dg_MainStocks.SelectedItem is StockDisplayModel;
            btn_MainEditSelected.IsEnabled = hasSelection;
            btn_MainHideSelected.IsEnabled = hasSelection;
        }

        private void dg_MainStocks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMainActionButtonsState();
        }

        private void dg_MainStocks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_MainStocks.SelectedItem is StockDisplayModel selected)
            {
                OpenInventoryUpdateModal(selected.StId);
            }
        }

        private void btn_MainNewRegister_Click(object sender, RoutedEventArgs e)
        {
            OpenInventoryRegisterModal();
        }

        private void btn_MainEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_MainStocks.SelectedItem is StockDisplayModel selected)
            {
                OpenInventoryUpdateModal(selected.StId);
            }
        }

        private void btn_MainHideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_MainStocks.SelectedItem is StockDisplayModel selected)
            {
                OpenInventoryHideModal(selected.StId);
            }
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
                ExecuteMainSearch();
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
        // 5.2 / 5.3: メイン画面で直接検索・一覧表示を行うためモーダル不要
        public void OpenInventoryListModal() => ExecuteMainSearch();
        public void OpenInventorySearchModal() => ExecuteMainSearch();

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

        // =====================================================================
        // 新規商品登録モーダル (在庫管理から呼び出し)
        // =====================================================================
        private void btn_MainNewProduct_Click(object sender, RoutedEventArgs e)
        {
            OpenInventoryCreateProductModal();
        }

        private void btn_QuickCreateProduct_Click(object sender, RoutedEventArgs e)
        {
            OpenInventoryCreateProductModal();
        }

        public void OpenInventoryCreateProductModal()
        {
            border_NewProdErrorAlert.Visibility = Visibility.Collapsed;
            border_NewProdSuccessAlert.Visibility = Visibility.Collapsed;

            var makers = _productService.GetMakers();
            newprod_cmb_Maker.ItemsSource = makers;
            if (makers.Count > 0) newprod_cmb_Maker.SelectedIndex = 0;

            var smallClasses = _productService.GetSmallClassifications();
            newprod_cmb_SmallClass.ItemsSource = smallClasses;
            if (smallClasses.Count > 0) newprod_cmb_SmallClass.SelectedIndex = 0;

            newprod_txt_PrName.Text = string.Empty;
            newprod_txt_Price.Text = string.Empty;
            newprod_txt_SafetyStock.Text = "10";
            newprod_txt_InitialStock.Text = "50";
            newprod_txt_ModelNumber.Text = string.Empty;
            newprod_txt_Color.Text = string.Empty;

            modal_InventoryCreateProduct.Visibility = Visibility.Visible;
            newprod_txt_PrName.Focus();
        }

        private void btn_CloseInventoryCreateProduct_Click(object sender, RoutedEventArgs e)
        {
            modal_InventoryCreateProduct.Visibility = Visibility.Collapsed;
        }

        private void modal_InventoryCreateProduct_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_InventoryCreateProduct.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteInventoryCreateProduct_Click(object sender, RoutedEventArgs e)
        {
            border_NewProdErrorAlert.Visibility = Visibility.Collapsed;
            border_NewProdSuccessAlert.Visibility = Visibility.Collapsed;

            string name = newprod_txt_PrName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowNewProdError("商品名を入力してください。");
                return;
            }

            if (!decimal.TryParse(newprod_txt_Price.Text.Trim(), out decimal price) || price < 0)
            {
                ShowNewProdError("価格には0以上の半角数値を入力してください。");
                return;
            }

            if (newprod_cmb_Maker.SelectedValue is not int maId || maId <= 0)
            {
                ShowNewProdError("メーカーを選択してください。");
                return;
            }

            if (newprod_cmb_SmallClass.SelectedValue is not int scId || scId <= 0)
            {
                ShowNewProdError("小分類を選択してください。");
                return;
            }

            if (!int.TryParse(newprod_txt_SafetyStock.Text.Trim(), out int safetyStock) || safetyStock < 0)
            {
                ShowNewProdError("安全在庫数には0以上の半角整数を入力してください。");
                return;
            }

            if (!int.TryParse(newprod_txt_InitialStock.Text.Trim(), out int initialStock) || initialStock < 0)
            {
                ShowNewProdError("初期在庫数には0以上の半角整数を入力してください。");
                return;
            }

            var newProduct = new MProduct
            {
                PrName = name,
                MaId = maId,
                ScId = scId,
                Price = price,
                PrSafetyStock = safetyStock,
                PrModelNumber = newprod_txt_ModelNumber.Text.Trim(),
                PrColor = newprod_txt_Color.Text.Trim(),
                PrReleaseDate = DateTime.Today
            };

            var result = _productService.RegisterProduct(newProduct);
            if (!result.Success)
            {
                ShowNewProdError(result.Message);
                return;
            }

            // 初期在庫数が設定されている場合は更新
            if (initialStock > 0 && result.Product != null)
            {
                _stockService.RegisterStock(result.Product.PrId, initialStock);
            }

            ShowNewProdSuccess($"商品「{newProduct.PrName}」を登録しました（初期在庫: {initialStock}個）。");
            ExecuteMainSearch();

            // 在庫登録モーダルが開いている場合は商品一覧を更新して作成した商品を選択
            if (modal_InventoryRegister.Visibility == Visibility.Visible && result.Product != null)
            {
                var products = _productService.GetAllProducts(includeHidden: false);
                reg_cmb_Product.ItemsSource = products;
                reg_cmb_Product.SelectedValue = result.Product.PrId;
                reg_txt_Quantity.Text = initialStock.ToString();
            }

            modal_InventoryCreateProduct.Visibility = Visibility.Collapsed;
        }

        private void ShowNewProdError(string msg)
        {
            txt_NewProdErrorMessage.Text = msg;
            border_NewProdErrorAlert.Visibility = Visibility.Visible;
        }

        private void ShowNewProdSuccess(string msg)
        {
            txt_NewProdSuccessMessage.Text = msg;
            border_NewProdSuccessAlert.Visibility = Visibility.Visible;
        }
    }
}
