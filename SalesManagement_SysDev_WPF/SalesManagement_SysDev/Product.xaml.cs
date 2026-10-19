using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class Product : UserControl
    {
        private readonly ProductService _productService = new();
        private List<ProductDisplayModel> _cachedProductList = new();

        public Product()
        {
            InitializeComponent();
        }

        // =====================================================================
        // カードクリック イベントハンドラ
        // =====================================================================
        private void card_ProductRegister_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenProductRegisterModal();
        }

        private void card_ProductUpdate_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenProductUpdateModal();
        }

        private void card_ProductList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenProductListModal();
        }

        private void card_ProductSearch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenProductSearchModal();
        }

        private void card_ProductHide_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenProductHideModal();
        }

        // =====================================================================
        // 1.1 商品登録モーダル
        // =====================================================================
        public void OpenProductRegisterModal()
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            // マスタ読み込み
            var makers = _productService.GetMakers();
            reg_cmb_Maker.ItemsSource = makers;
            if (makers.Count > 0) reg_cmb_Maker.SelectedIndex = 0;

            var smallClasses = _productService.GetSmallClassifications();
            reg_cmb_SmallClass.ItemsSource = smallClasses;
            if (smallClasses.Count > 0) reg_cmb_SmallClass.SelectedIndex = 0;

            // フォームリセット
            reg_txt_PrName.Text = "";
            reg_txt_Price.Text = "";
            reg_txt_ModelNumber.Text = "";
            reg_txt_Color.Text = "";
            reg_txt_SafetyStock.Text = "0";
            reg_dp_ReleaseDate.SelectedDate = DateTime.Today;
            reg_txt_Jcode.Text = "";

            modal_ProductRegister.Visibility = Visibility.Visible;
            reg_txt_PrName.Focus();
        }

        private void btn_CloseProductRegister_Click(object sender, RoutedEventArgs e)
        {
            modal_ProductRegister.Visibility = Visibility.Collapsed;
        }

        private void modal_ProductRegister_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ProductRegister.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteProductRegister_Click(object sender, RoutedEventArgs e)
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            // 入力チェック
            string name = reg_txt_PrName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowRegError("商品名を入力してください。");
                return;
            }

            if (!decimal.TryParse(reg_txt_Price.Text.Trim(), out decimal price) || price < 0)
            {
                ShowRegError("価格には0以上の半角数値を入力してください。");
                return;
            }

            if (reg_cmb_Maker.SelectedValue is not int maId || maId <= 0)
            {
                ShowRegError("メーカーを選択してください。");
                return;
            }

            if (reg_cmb_SmallClass.SelectedValue is not int scId || scId <= 0)
            {
                ShowRegError("小分類を選択してください。");
                return;
            }

            if (!int.TryParse(reg_txt_SafetyStock.Text.Trim(), out int safetyStock) || safetyStock < 0)
            {
                ShowRegError("安全在庫数には0以上の半角整数を入力してください。");
                return;
            }

            DateTime releaseDate = reg_dp_ReleaseDate.SelectedDate ?? DateTime.Today;

            var newProduct = new MProduct
            {
                PrName = name,
                MaId = maId,
                ScId = scId,
                Price = price,
                PrSafetyStock = safetyStock,
                PrModelNumber = reg_txt_ModelNumber.Text.Trim(),
                PrColor = reg_txt_Color.Text.Trim(),
                PrReleaseDate = releaseDate,
                PrJcode = string.IsNullOrWhiteSpace(reg_txt_Jcode.Text) ? null : reg_txt_Jcode.Text.Trim()
            };

            var result = _productService.RegisterProduct(newProduct);
            if (result.Success)
            {
                ShowRegSuccess(result.Message);
                // 入力欄クリア
                reg_txt_PrName.Text = "";
                reg_txt_Price.Text = "";
                reg_txt_ModelNumber.Text = "";
                reg_txt_Color.Text = "";
                reg_txt_SafetyStock.Text = "0";
                reg_txt_Jcode.Text = "";
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
        // 4.2 商品更新モーダル
        // =====================================================================
        public void OpenProductUpdateModal(int? targetPrId = null)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            // マスタ読み込み
            var makers = _productService.GetMakers();
            upd_cmb_Maker.ItemsSource = makers;

            var smallClasses = _productService.GetSmallClassifications();
            upd_cmb_SmallClass.ItemsSource = smallClasses;

            ClearUpdateFields();

            upd_txt_SearchPrId.Text = targetPrId.HasValue ? targetPrId.Value.ToString() : "";
            modal_ProductUpdate.Visibility = Visibility.Visible;

            if (targetPrId.HasValue && targetPrId.Value > 0)
            {
                LoadProductForUpdate(targetPrId.Value);
            }
            else
            {
                upd_txt_SearchPrId.Focus();
            }
        }

        private void ClearUpdateFields()
        {
            upd_txt_PrName.Text = "";
            upd_txt_Price.Text = "";
            upd_txt_ModelNumber.Text = "";
            upd_txt_Color.Text = "";
            upd_txt_SafetyStock.Text = "";
            upd_dp_ReleaseDate.SelectedDate = null;
            upd_cmb_PrFlag.SelectedIndex = 0;
            upd_txt_PrHidden.Text = "";
        }

        private void btn_CloseProductUpdate_Click(object sender, RoutedEventArgs e)
        {
            modal_ProductUpdate.Visibility = Visibility.Collapsed;
        }

        private void modal_ProductUpdate_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ProductUpdate.Visibility = Visibility.Collapsed;
        }

        private void upd_txt_SearchPrId_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_LoadProductForUpdate_Click(sender, e);
            }
        }

        private void btn_LoadProductForUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchPrId.Text.Trim(), out int prId) || prId <= 0)
            {
                ShowUpdError("有効な商品IDを半角数値で入力してください。");
                return;
            }

            LoadProductForUpdate(prId);
        }

        private void LoadProductForUpdate(int prId)
        {
            var product = _productService.GetProductById(prId);
            if (product == null)
            {
                ShowUpdError($"商品ID: {prId} の商品は見つかりませんでした。");
                ClearUpdateFields();
                return;
            }

            upd_txt_SearchPrId.Text = product.PrId.ToString();
            upd_txt_PrName.Text = product.PrName;
            upd_cmb_Maker.SelectedValue = product.MaId;
            upd_cmb_SmallClass.SelectedValue = product.ScId;
            upd_txt_Price.Text = product.Price.ToString("F0");
            upd_txt_ModelNumber.Text = product.PrModelNumber;
            upd_txt_Color.Text = product.PrColor;
            upd_txt_SafetyStock.Text = product.PrSafetyStock.ToString();
            upd_dp_ReleaseDate.SelectedDate = product.PrReleaseDate;
            upd_cmb_PrFlag.SelectedIndex = product.PrFlag == 2 ? 1 : 0;
            upd_txt_PrHidden.Text = product.PrHidden ?? "";
        }

        private void btn_ExecuteProductUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchPrId.Text.Trim(), out int prId) || prId <= 0)
            {
                ShowUpdError("更新対象の商品IDを入力して情報を呼び出してください。");
                return;
            }

            string name = upd_txt_PrName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowUpdError("商品名を入力してください。");
                return;
            }

            if (!decimal.TryParse(upd_txt_Price.Text.Trim(), out decimal price) || price < 0)
            {
                ShowUpdError("価格には0以上の半角数値を入力してください。");
                return;
            }

            if (upd_cmb_Maker.SelectedValue is not int maId || maId <= 0)
            {
                ShowUpdError("メーカーを選択してください。");
                return;
            }

            if (upd_cmb_SmallClass.SelectedValue is not int scId || scId <= 0)
            {
                ShowUpdError("小分類を選択してください。");
                return;
            }

            if (!int.TryParse(upd_txt_SafetyStock.Text.Trim(), out int safetyStock) || safetyStock < 0)
            {
                ShowUpdError("安全在庫数には0以上の半角整数を入力してください。");
                return;
            }

            int prFlag = upd_cmb_PrFlag.SelectedIndex == 1 ? 2 : 0;
            string? hidden = string.IsNullOrWhiteSpace(upd_txt_PrHidden.Text) ? null : upd_txt_PrHidden.Text.Trim();

            var updateProduct = new MProduct
            {
                PrId = prId,
                PrName = name,
                MaId = maId,
                ScId = scId,
                Price = price,
                PrSafetyStock = safetyStock,
                PrModelNumber = upd_txt_ModelNumber.Text.Trim(),
                PrColor = upd_txt_Color.Text.Trim(),
                PrReleaseDate = upd_dp_ReleaseDate.SelectedDate ?? DateTime.Today,
                PrFlag = prFlag,
                PrHidden = hidden
            };

            var result = _productService.UpdateProduct(updateProduct);
            if (result.Success)
            {
                ShowUpdSuccess(result.Message);
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
        // 4.3 商品一覧表示モーダル
        // =====================================================================
        public void OpenProductListModal()
        {
            txt_ListFilterKeyword.Text = "";
            chk_ListIncludeHidden.IsChecked = false;
            modal_ProductList.Visibility = Visibility.Visible;
            LoadProductList();
        }

        private void LoadProductList()
        {
            bool includeHidden = chk_ListIncludeHidden.IsChecked == true;
            _cachedProductList = _productService.GetAllProducts(includeHidden);
            ApplyProductListFilter();
        }

        private void ApplyProductListFilter()
        {
            string keyword = (txt_ListFilterKeyword.Text ?? "").Trim();
            var filtered = _cachedProductList;

            if (!string.IsNullOrEmpty(keyword))
            {
                filtered = filtered.Where(p =>
                    p.PrName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.PrId.ToString().Contains(keyword) ||
                    p.MaName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.ScName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.PrModelNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.PrColor.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            dg_ProductList.ItemsSource = filtered;
            txt_ListCount.Text = $"表示件数: {filtered.Count} 件 (全 {_cachedProductList.Count} 件中)";
        }

        private void txt_ListFilterKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            ApplyProductListFilter();
        }

        private void chk_ListIncludeHidden_Click(object sender, RoutedEventArgs e)
        {
            LoadProductList();
        }

        private void btn_ListRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadProductList();
        }

        private void btn_CloseProductList_Click(object sender, RoutedEventArgs e)
        {
            modal_ProductList.Visibility = Visibility.Collapsed;
        }

        private void modal_ProductList_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ProductList.Visibility = Visibility.Collapsed;
        }

        private void dg_ProductList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_ProductList.SelectedItem is ProductDisplayModel selected)
            {
                modal_ProductList.Visibility = Visibility.Collapsed;
                OpenProductUpdateModal(selected.PrId);
            }
        }

        private void btn_ListEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_ProductList.SelectedItem is ProductDisplayModel selected)
            {
                modal_ProductList.Visibility = Visibility.Collapsed;
                OpenProductUpdateModal(selected.PrId);
            }
            else
            {
                MessageBox.Show("一覧から編集する商品を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btn_ListHideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_ProductList.SelectedItem is ProductDisplayModel selected)
            {
                modal_ProductList.Visibility = Visibility.Collapsed;
                OpenProductHideModal(selected.PrId);
            }
            else
            {
                MessageBox.Show("一覧から非表示にする商品を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =====================================================================
        // 4.4 商品検索モーダル
        // =====================================================================
        public void OpenProductSearchModal()
        {
            // マスタ選択肢設定
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

            srch_txt_PrId.Text = "";
            srch_txt_PrName.Text = "";
            srch_txt_MinPrice.Text = "";
            srch_txt_MaxPrice.Text = "";

            modal_ProductSearch.Visibility = Visibility.Visible;
            ExecuteProductSearch();
        }

        private void btn_CloseProductSearch_Click(object sender, RoutedEventArgs e)
        {
            modal_ProductSearch.Visibility = Visibility.Collapsed;
        }

        private void modal_ProductSearch_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ProductSearch.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteProductSearch_Click(object sender, RoutedEventArgs e)
        {
            ExecuteProductSearch();
        }

        private void ExecuteProductSearch()
        {
            int? prId = null;
            if (int.TryParse(srch_txt_PrId.Text.Trim(), out int parsedId) && parsedId > 0)
            {
                prId = parsedId;
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

            decimal? minPrice = null;
            if (decimal.TryParse(srch_txt_MinPrice.Text.Trim(), out decimal pMin))
            {
                minPrice = pMin;
            }

            decimal? maxPrice = null;
            if (decimal.TryParse(srch_txt_MaxPrice.Text.Trim(), out decimal pMax))
            {
                maxPrice = pMax;
            }

            var results = _productService.SearchProducts(
                prId: prId,
                prName: name,
                maId: maId,
                scId: scId,
                minPrice: minPrice,
                maxPrice: maxPrice,
                includeHidden: false);

            dg_ProductSearchResults.ItemsSource = results;
            srch_txt_Count.Text = $"抽出件数: {results.Count} 件";
        }

        private void btn_ClearProductSearch_Click(object sender, RoutedEventArgs e)
        {
            srch_txt_PrId.Text = "";
            srch_txt_PrName.Text = "";
            srch_cmb_Maker.SelectedIndex = 0;
            srch_cmb_SmallClass.SelectedIndex = 0;
            srch_txt_MinPrice.Text = "";
            srch_txt_MaxPrice.Text = "";
            ExecuteProductSearch();
        }

        private void dg_ProductSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_ProductSearchResults.SelectedItem is ProductDisplayModel selected)
            {
                modal_ProductSearch.Visibility = Visibility.Collapsed;
                OpenProductUpdateModal(selected.PrId);
            }
        }

        private void btn_SearchEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_ProductSearchResults.SelectedItem is ProductDisplayModel selected)
            {
                modal_ProductSearch.Visibility = Visibility.Collapsed;
                OpenProductUpdateModal(selected.PrId);
            }
            else
            {
                MessageBox.Show("一覧から編集する商品を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =====================================================================
        // 4.5 商品非表示モーダル (フラグ 0 -> 2 更新)
        // =====================================================================
        public void OpenProductHideModal(int? targetPrId = null)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            hide_txt_PrId.Text = targetPrId.HasValue ? targetPrId.Value.ToString() : "";
            hide_txt_DisplayPrName.Text = "-";
            hide_txt_DisplayMaker.Text = "-";
            hide_txt_DisplayPrice.Text = "-";
            hide_txt_DisplayFlag.Text = "-";
            hide_txt_Reason.Text = "";

            modal_ProductHide.Visibility = Visibility.Visible;

            if (targetPrId.HasValue && targetPrId.Value > 0)
            {
                LoadProductForHide(targetPrId.Value);
            }
            else
            {
                hide_txt_PrId.Focus();
            }
        }

        private void btn_CloseProductHide_Click(object sender, RoutedEventArgs e)
        {
            modal_ProductHide.Visibility = Visibility.Collapsed;
        }

        private void modal_ProductHide_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ProductHide.Visibility = Visibility.Collapsed;
        }

        private void hide_txt_PrId_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_LoadProductForHide_Click(sender, e);
            }
        }

        private void btn_LoadProductForHide_Click(object sender, RoutedEventArgs e)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(hide_txt_PrId.Text.Trim(), out int prId) || prId <= 0)
            {
                ShowHideError("有効な商品IDを入力してください。");
                return;
            }

            LoadProductForHide(prId);
        }

        private void LoadProductForHide(int prId)
        {
            var product = _productService.GetProductById(prId);
            if (product == null)
            {
                ShowHideError($"商品ID: {prId} のデータが見つかりません。");
                hide_txt_DisplayPrName.Text = "-";
                hide_txt_DisplayMaker.Text = "-";
                hide_txt_DisplayPrice.Text = "-";
                hide_txt_DisplayFlag.Text = "-";
                return;
            }

            hide_txt_PrId.Text = product.PrId.ToString();
            hide_txt_DisplayPrName.Text = product.PrName;
            hide_txt_DisplayMaker.Text = $"{product.Ma?.MaName ?? "-"} / {product.Sc?.ScName ?? "-"}";
            hide_txt_DisplayPrice.Text = $"{product.Price:N0} 円";
            hide_txt_DisplayFlag.Text = product.PrFlag == 2 ? "2 (すでに非表示)" : "0 (通常表示中)";
            hide_txt_Reason.Text = product.PrHidden ?? "";
        }

        private void btn_ExecuteProductHide_Click(object sender, RoutedEventArgs e)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(hide_txt_PrId.Text.Trim(), out int prId) || prId <= 0)
            {
                ShowHideError("対象の商品IDを入力して情報を呼び出してください。");
                return;
            }

            var confirm = MessageBox.Show(
                $"商品ID: {prId}「{hide_txt_DisplayPrName.Text}」の商品管理フラグを 2 (非表示) に更新しますか？\n（更新後は通常の一覧や検索に表示されなくなります）",
                "非表示更新の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            string reason = hide_txt_Reason.Text.Trim();
            var result = _productService.SetProductHiddenFlag(prId, string.IsNullOrEmpty(reason) ? "管理者による非表示設定" : reason);

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