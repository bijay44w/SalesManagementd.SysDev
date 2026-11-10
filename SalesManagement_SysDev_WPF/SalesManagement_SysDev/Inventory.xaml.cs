
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 在庫管理画面
    /// 在庫登録・更新・一覧・検索・非表示を管理する。
    /// </summary>
    public partial class Inventory : UserControl
    {
        private readonly StockService _stockService = new();
        private readonly ProductService _productService = new();

        private System.Collections.Generic.List<StockDisplayModel>
            _cachedStockList = new();

        private int _selectedStockId = 0;

        public Inventory()
        {
            InitializeComponent();
            Loaded += Inventory_Loaded;
        }

        // =========================================================
        // 初期表示
        // =========================================================

        private void Inventory_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMainMasters();
            LoadRegisterProducts();
            ExecuteMainSearch();
            UpdateMainActionButtonsState();
        }

        private void LoadMainMasters()
        {
            try
            {
                cmb_MainMaker.ItemsSource = _productService.GetMakers();
                cmb_MainMaker.SelectedIndex = -1;

                newprod_cmb_Maker.ItemsSource =
                    _productService.GetMakers();

                newprod_cmb_SmallClass.ItemsSource =
                    _productService.GetSmallClassifications();
            }
            catch (Exception ex)
            {
                ShowError("マスタ情報の取得に失敗しました。", ex);
            }
        }

        private void LoadRegisterProducts()
        {
            try
            {
                reg_cmb_Product.ItemsSource =
                    _productService.GetAllProducts(false);

                reg_cmb_Product.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("商品一覧の取得に失敗しました。", ex);
            }
        }

        // =========================================================
        // メイン一覧・検索
        // =========================================================

        private void ExecuteMainSearch()
        {
            try
            {
                int? stockId = null;

                if (!string.IsNullOrWhiteSpace(txt_MainStId.Text))
                {
                    if (!int.TryParse(txt_MainStId.Text, out int id)
                        || id <= 0)
                    {
                        ShowMessage("在庫IDは正しい数値で入力してください。");
                        return;
                    }

                    stockId = id;
                }

                int? makerId = null;

                if (cmb_MainMaker.SelectedValue != null)
                {
                    makerId = Convert.ToInt32(
                        cmb_MainMaker.SelectedValue);
                }

                bool includeHidden =
                    chk_MainIncludeHidden.IsChecked == true;

                _cachedStockList = _stockService.SearchStocks(
                    stId: stockId,
                    prName: txt_MainPrName.Text,
                    maId: makerId,
                    includeHidden: includeHidden
                );

                if (chk_MainBelowSafety.IsChecked == true)
                {
                    _cachedStockList = _cachedStockList
                        .Where(s => s.StQuantity < s.PrSafetyStock)
                        .ToList();
                }

                dg_MainStocks.ItemsSource = null;
                dg_MainStocks.ItemsSource = _cachedStockList;

                txt_MainStockCount.Text =
                    $"該当件数：{_cachedStockList.Count}件";

                UpdateMainActionButtonsState();
            }
            catch (Exception ex)
            {
                ShowError("在庫検索に失敗しました。", ex);
            }
        }

        private void SearchField_KeyDown(
            object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteMainSearch();
            }
        }

        private void chk_Filter_Click(
            object sender, RoutedEventArgs e)
        {
            ExecuteMainSearch();
        }

        private void btn_MainSearch_Click(
            object sender, RoutedEventArgs e)
        {
            ExecuteMainSearch();
        }

        private void btn_MainClear_Click(
            object sender, RoutedEventArgs e)
        {
            txt_MainStId.Clear();
            txt_MainPrName.Clear();

            cmb_MainMaker.SelectedIndex = -1;
            chk_MainBelowSafety.IsChecked = false;
            chk_MainIncludeHidden.IsChecked = false;

            ExecuteMainSearch();
        }

        private void dg_MainStocks_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            UpdateMainActionButtonsState();
        }

        private void dg_MainStocks_MouseDoubleClick(
            object sender, MouseButtonEventArgs e)
        {
            if (dg_MainStocks.SelectedItem is StockDisplayModel stock)
            {
                OpenInventoryUpdate(stock);
            }
        }

        private void UpdateMainActionButtonsState()
        {
            bool selected =
                dg_MainStocks.SelectedItem is StockDisplayModel;

            btn_MainEditSelected.IsEnabled = selected;
            btn_MainHideSelected.IsEnabled = selected;
        }

        // =========================================================
        // メイン操作ボタン
        // =========================================================

        private void btn_MainNewRegister_Click(
            object sender, RoutedEventArgs e)
        {
            OpenInventoryRegister();
        }

        private void btn_MainNewProduct_Click(
            object sender, RoutedEventArgs e)
        {
            OpenInventoryCreateProduct();
        }

        private void btn_MainEditSelected_Click(
            object sender, RoutedEventArgs e)
        {
            if (dg_MainStocks.SelectedItem is StockDisplayModel stock)
            {
                OpenInventoryUpdate(stock);
            }
            else
            {
                ShowMessage("更新する在庫を選択してください。");
            }
        }

        private void btn_MainHideSelected_Click(
            object sender, RoutedEventArgs e)
        {
            if (dg_MainStocks.SelectedItem is StockDisplayModel stock)
            {
                OpenInventoryHide(stock);
            }
            else
            {
                ShowMessage("非表示にする在庫を選択してください。");
            }
        }

        // =========================================================
        // 在庫登録
        // =========================================================

        private void OpenInventoryRegister()
        {
            ClearRegisterMessage();

            reg_txt_Quantity.Text = "0";
            reg_cmb_Product.SelectedIndex = -1;

            modal_InventoryRegister.Visibility =
                Visibility.Visible;
        }

        private void modal_InventoryRegister_Backdrop_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                CloseInventoryRegister();
            }
        }

        private void btn_CloseInventoryRegister_Click(
            object sender, RoutedEventArgs e)
        {
            CloseInventoryRegister();
        }

        private void CloseInventoryRegister()
        {
            modal_InventoryRegister.Visibility =
                Visibility.Collapsed;
        }

        private void reg_cmb_Product_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (reg_cmb_Product.SelectedItem is ProductDisplayModel product)
            {
                reg_txt_ProductInfo.Text =
                    $"{product.MaName} / {product.ScName}";

                reg_txt_SafetyStock.Text =
                    product.PrSafetyStock.ToString();
            }
            else
            {
                reg_txt_ProductInfo.Text = "-";
                reg_txt_SafetyStock.Text = "-";
            }
        }

        private void btn_ExecuteInventoryRegister_Click(
            object sender, RoutedEventArgs e)
        {
            if (reg_cmb_Product.SelectedValue == null)
            {
                ShowRegisterError("商品を選択してください。");
                return;
            }

            if (!int.TryParse(reg_txt_Quantity.Text, out int quantity)
                || quantity < 0)
            {
                ShowRegisterError(
                    "在庫数は0以上の整数で入力してください。");
                return;
            }

            int productId =
                Convert.ToInt32(reg_cmb_Product.SelectedValue);

            var result =
                _stockService.RegisterStock(productId, quantity);

            if (!result.Success)
            {
                ShowRegisterError(result.Message);
                return;
            }

            ShowRegisterSuccess(result.Message);

            ExecuteMainSearch();
            LoadRegisterProducts();

            ShowMessage(result.Message);
            CloseInventoryRegister();
        }

        // =========================================================
        // 在庫更新
        // =========================================================

        private void OpenInventoryUpdate(StockDisplayModel stock)
        {
            _selectedStockId = stock.StId;

            ClearUpdateMessage();

            upd_txt_SearchStId.Text = stock.StId.ToString();

            LoadStockForUpdate(stock.StId);

            modal_InventoryUpdate.Visibility =
                Visibility.Visible;
        }

        private void modal_InventoryUpdate_Backdrop_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                CloseInventoryUpdate();
            }
        }

        private void btn_CloseInventoryUpdate_Click(
            object sender, RoutedEventArgs e)
        {
            CloseInventoryUpdate();
        }

        private void CloseInventoryUpdate()
        {
            modal_InventoryUpdate.Visibility =
                Visibility.Collapsed;

            _selectedStockId = 0;
        }

        private void upd_txt_SearchStId_KeyUp(
            object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadStockForUpdateFromText();
            }
        }

        private void btn_LoadStockForUpdate_Click(
            object sender, RoutedEventArgs e)
        {
            LoadStockForUpdateFromText();
        }

        private void LoadStockForUpdateFromText()
        {
            if (!int.TryParse(
                upd_txt_SearchStId.Text, out int stockId)
                || stockId <= 0)
            {
                ShowUpdateError("正しい在庫IDを入力してください。");
                return;
            }

            LoadStockForUpdate(stockId);
        }

        private void LoadStockForUpdate(int stockId)
        {
            var stock = _stockService.GetStockById(stockId);

            if (stock == null)
            {
                ShowUpdateError("対象の在庫が見つかりません。");
                return;
            }

            var display = _stockService.ToDisplayModel(stock);

            _selectedStockId = stock.StId;

            upd_txt_DisplayPrName.Text = display.PrName;
            upd_txt_DisplayMaker.Text = display.MaName;
            upd_txt_DisplaySafetyStock.Text =
                display.PrSafetyStock.ToString();

            upd_txt_DisplayCurrentQty.Text =
                display.StQuantity.ToString();

            upd_txt_Quantity.Text =
                display.StQuantity.ToString();

            foreach (ComboBoxItem item in upd_cmb_StFlag.Items)
            {
                if (Convert.ToInt32(item.Tag) == stock.StFlag)
                {
                    upd_cmb_StFlag.SelectedItem = item;
                    break;
                }
            }
        }

        private void btn_ExecuteInventoryUpdate_Click(
            object sender, RoutedEventArgs e)
        {
            if (_selectedStockId <= 0)
            {
                ShowUpdateError("更新対象の在庫を読み込んでください。");
                return;
            }

            if (!int.TryParse(
                upd_txt_Quantity.Text, out int quantity)
                || quantity < 0)
            {
                ShowUpdateError(
                    "在庫数は0以上の整数で入力してください。");
                return;
            }

            if (upd_cmb_StFlag.SelectedItem is not ComboBoxItem flagItem)
            {
                ShowUpdateError("在庫管理フラグを選択してください。");
                return;
            }

            int flag = Convert.ToInt32(flagItem.Tag);

            var result = _stockService.UpdateStock(
                _selectedStockId, quantity, flag);

            if (!result.Success)
            {
                ShowUpdateError(result.Message);
                return;
            }

            ShowMessage(result.Message);

            ExecuteMainSearch();
            CloseInventoryUpdate();
        }

        // =========================================================
        // 在庫非表示
        // =========================================================

        private void OpenInventoryHide(StockDisplayModel stock)
        {
            _selectedStockId = stock.StId;

            ClearHideMessage();

            hide_txt_StId.Text = stock.StId.ToString();

            LoadStockForHide(stock.StId);

            modal_InventoryHide.Visibility =
                Visibility.Visible;
        }

        private void modal_InventoryHide_Backdrop_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                CloseInventoryHide();
            }
        }

        private void btn_CloseInventoryHide_Click(
            object sender, RoutedEventArgs e)
        {
            CloseInventoryHide();
        }

        private void CloseInventoryHide()
        {
            modal_InventoryHide.Visibility =
                Visibility.Collapsed;

            _selectedStockId = 0;
        }

        private void hide_txt_StId_KeyUp(
            object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadStockForHideFromText();
            }
        }

        private void btn_LoadStockForHide_Click(
            object sender, RoutedEventArgs e)
        {
            LoadStockForHideFromText();
        }

        private void LoadStockForHideFromText()
        {
            if (!int.TryParse(
                hide_txt_StId.Text, out int stockId)
                || stockId <= 0)
            {
                ShowHideError("正しい在庫IDを入力してください。");
                return;
            }

            LoadStockForHide(stockId);
        }

        private void LoadStockForHide(int stockId)
        {
            var stock = _stockService.GetStockById(stockId);

            if (stock == null)
            {
                ShowHideError("対象の在庫が見つかりません。");
                return;
            }

            var display = _stockService.ToDisplayModel(stock);

            _selectedStockId = stock.StId;

            hide_txt_DisplayPrName.Text = display.PrName;
            hide_txt_DisplayMaker.Text = display.MaName;
            hide_txt_DisplayQty.Text =
                display.StQuantity.ToString();

            hide_txt_DisplayFlag.Text =
                display.FlagFormatted;
        }

        private void btn_ExecuteInventoryHide_Click(
            object sender, RoutedEventArgs e)
        {
            if (_selectedStockId <= 0)
            {
                ShowHideError("非表示にする在庫を読み込んでください。");
                return;
            }

            var answer = MessageBox.Show(
                "この在庫を非表示にしますか？",
                "在庫非表示確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            var result =
                _stockService.SetStockHiddenFlag(_selectedStockId);

            if (!result.Success)
            {
                ShowHideError(result.Message);
                return;
            }

            ShowMessage(result.Message);

            ExecuteMainSearch();
            CloseInventoryHide();
        }

        // =========================================================
        // 商品新規登録
        // =========================================================

        private void OpenInventoryCreateProduct()
        {
            ClearNewProductMessage();

            newprod_txt_PrName.Clear();
            newprod_txt_Price.Clear();
            newprod_txt_ModelNumber.Clear();
            newprod_txt_Color.Clear();

            newprod_txt_SafetyStock.Text = "10";
            newprod_txt_InitialStock.Text = "50";

            newprod_cmb_Maker.SelectedIndex = -1;
            newprod_cmb_SmallClass.SelectedIndex = -1;

            modal_InventoryCreateProduct.Visibility =
                Visibility.Visible;
        }

        private void btn_QuickCreateProduct_Click(
            object sender, RoutedEventArgs e)
        {
            OpenInventoryCreateProduct();
        }

        private void modal_InventoryCreateProduct_Backdrop_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                CloseInventoryCreateProduct();
            }
        }

        private void btn_CloseInventoryCreateProduct_Click(
            object sender, RoutedEventArgs e)
        {
            CloseInventoryCreateProduct();
        }

        private void CloseInventoryCreateProduct()
        {
            modal_InventoryCreateProduct.Visibility =
                Visibility.Collapsed;
        }

        private void btn_ExecuteInventoryCreateProduct_Click(
            object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newprod_txt_PrName.Text))
            {
                ShowNewProdError("商品名を入力してください。");
                return;
            }

            if (!decimal.TryParse(
                newprod_txt_Price.Text, out decimal price)
                || price < 0)
            {
                ShowNewProdError("価格を正しく入力してください。");
                return;
            }

            if (newprod_cmb_Maker.SelectedValue == null)
            {
                ShowNewProdError("メーカーを選択してください。");
                return;
            }

            if (newprod_cmb_SmallClass.SelectedValue == null)
            {
                ShowNewProdError("小分類を選択してください。");
                return;
            }

            if (!int.TryParse(
                newprod_txt_SafetyStock.Text, out int safetyStock)
                || safetyStock < 0)
            {
                ShowNewProdError("安全在庫数を正しく入力してください。");
                return;
            }

            if (!int.TryParse(
                newprod_txt_InitialStock.Text, out int initialStock)
                || initialStock < 0)
            {
                ShowNewProdError("初期在庫数を正しく入力してください。");
                return;
            }

            var product = new MProduct
            {
                PrName = newprod_txt_PrName.Text.Trim(),
                Price = price,
                MaId = Convert.ToInt32(
                    newprod_cmb_Maker.SelectedValue),
                ScId = Convert.ToInt32(
                    newprod_cmb_SmallClass.SelectedValue),
                PrSafetyStock = safetyStock,
                PrModelNumber =
                    newprod_txt_ModelNumber.Text.Trim(),
                PrColor =
                    newprod_txt_Color.Text.Trim(),
                PrReleaseDate = DateTime.Today,
                PrFlag = 0
            };

            var result =
                _productService.RegisterProduct(product);

            if (!result.Success || result.Product == null)
            {
                ShowNewProdError(result.Message);
                return;
            }

            // 商品登録時に在庫0のレコードが自動生成される。
            // そのため、在庫登録サービスで初期数量を追加する。
            var stockResult = _stockService.RegisterStock(
                result.Product.PrId, initialStock);

            if (!stockResult.Success)
            {
                ShowNewProdError(
                    "商品は登録されましたが、初期在庫の登録に失敗しました。\n"
                    + stockResult.Message);
                return;
            }

            ShowMessage(
                "商品と初期在庫を登録しました。\n"
                + result.Message);

            LoadRegisterProducts();
            ExecuteMainSearch();

            CloseInventoryCreateProduct();
        }

        // =========================================================
        // メッセージ表示
        // =========================================================

        private void ShowMessage(string message)
        {
            MessageBox.Show(
                message,
                "在庫管理",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show(
                message + "\n" + ex.Message,
                "在庫管理エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void ShowRegisterError(string message)
        {
            border_RegErrorAlert.Visibility = Visibility.Visible;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;
            txt_RegErrorMessage.Text = message;
        }

        private void ShowRegisterSuccess(string message)
        {
            border_RegSuccessAlert.Visibility = Visibility.Visible;
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            txt_RegSuccessMessage.Text = message;
        }

        private void ClearRegisterMessage()
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;
        }

        private void ShowUpdateError(string message)
        {
            border_UpdErrorAlert.Visibility = Visibility.Visible;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;
            txt_UpdErrorMessage.Text = message;
        }

        private void ClearUpdateMessage()
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;
        }

        private void ShowHideError(string message)
        {
            border_HideErrorAlert.Visibility = Visibility.Visible;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;
            txt_HideErrorMessage.Text = message;
        }

        private void ClearHideMessage()
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;
        }

        private void ShowNewProdError(string message)
        {
            border_NewProdErrorAlert.Visibility = Visibility.Visible;
            border_NewProdSuccessAlert.Visibility = Visibility.Collapsed;
            txt_NewProdErrorMessage.Text = message;
        }

        private void ClearNewProductMessage()
        {
            border_NewProdErrorAlert.Visibility = Visibility.Collapsed;
            border_NewProdSuccessAlert.Visibility = Visibility.Collapsed;
        }
    }
}
