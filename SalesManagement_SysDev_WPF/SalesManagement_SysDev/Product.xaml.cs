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
            Loaded += Product_Loaded;
        }

        private void Product_Loaded(object sender, RoutedEventArgs e)
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

            var smallClasses = _productService.GetSmallClassifications();
            var searchSmall = new List<MSmallClassification> { new MSmallClassification { ScId = 0, ScName = "すべての小分類" } };
            searchSmall.AddRange(smallClasses);
            cmb_MainSmallClass.ItemsSource = searchSmall;
            cmb_MainSmallClass.SelectedIndex = 0;
        }

        private void ExecuteMainSearch()
        {
            int? prId = null;
            if (int.TryParse(txt_MainPrId.Text.Trim(), out int id)) prId = id;

            string name = txt_MainPrName.Text.Trim();

            int? maId = null;
            if (cmb_MainMaker.SelectedValue is int mId && mId > 0) maId = mId;

            int? scId = null;
            if (cmb_MainSmallClass.SelectedValue is int sId && sId > 0) scId = sId;

            decimal? minPrice = null;
            if (decimal.TryParse(txt_MainMinPrice.Text.Trim(), out decimal min)) minPrice = min;

            decimal? maxPrice = null;
            if (decimal.TryParse(txt_MainMaxPrice.Text.Trim(), out decimal max)) maxPrice = max;

            bool includeHidden = chk_MainIncludeHidden.IsChecked == true;

            var results = _productService.SearchProducts(prId, name, maId, scId, mcId: null, minPrice, maxPrice, includeHidden);
            _cachedProductList = results;
            dg_MainProducts.ItemsSource = _cachedProductList;
            txt_MainProductCount.Text = $"表示件数: {_cachedProductList.Count} 件";
            UpdateMainActionButtonsState();
        }

        private void btn_MainSearch_Click(object sender, RoutedEventArgs e)
        {
            ExecuteMainSearch();
        }

        private void btn_MainClear_Click(object sender, RoutedEventArgs e)
        {
            txt_MainPrId.Text = string.Empty;
            txt_MainPrName.Text = string.Empty;
            cmb_MainMaker.SelectedIndex = 0;
            cmb_MainSmallClass.SelectedIndex = 0;
            txt_MainMinPrice.Text = string.Empty;
            txt_MainMaxPrice.Text = string.Empty;
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

        private void chk_MainIncludeHidden_Click(object sender, RoutedEventArgs e)
        {
            ExecuteMainSearch();
        }

        private void UpdateMainActionButtonsState()
        {
            bool hasSelection = dg_MainProducts.SelectedItem is ProductDisplayModel;
            btn_MainEditSelected.IsEnabled = hasSelection;
            btn_MainHideSelected.IsEnabled = hasSelection;
        }

        private void dg_MainProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMainActionButtonsState();
        }

        private void dg_MainProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_MainProducts.SelectedItem is ProductDisplayModel selected)
            {
                OpenProductUpdateModal(selected.PrId);
            }
        }

        private void btn_MainNewRegister_Click(object sender, RoutedEventArgs e)
        {
            OpenProductRegisterModal();
        }

        private void btn_MainEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_MainProducts.SelectedItem is ProductDisplayModel selected)
            {
                OpenProductUpdateModal(selected.PrId);
            }
        }

        private void btn_MainHideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_MainProducts.SelectedItem is ProductDisplayModel selected)
            {
                OpenProductHideModal(selected.PrId);
            }
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
        // 4.3 / 4.4: メイン画面で直接検索・一覧表示を行うためモーダル不要
        public void OpenProductListModal() => ExecuteMainSearch();
        public void OpenProductSearchModal() => ExecuteMainSearch();

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