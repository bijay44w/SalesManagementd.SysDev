using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class Customer : UserControl
    {
        private readonly ClientService _clientService = new();
        private List<ClientDisplayModel> _cachedList = new();
        private List<MSalesOffice> _offices = new();

        // ClientRegister handled within Customer directly

        public Customer()
        {
            InitializeComponent();
            Loaded += Customer_Loaded;
        }

        private void Customer_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOffices();
            ExecuteSearch();
            UpdateActionButtonsState();
        }

        private void LoadOffices()
        {
            _offices = _clientService.GetSalesOffices();

            // 検索コンボ用 (先頭に「すべて」を追加)
            var searchOffices = new List<MSalesOffice>
            {
                new MSalesOffice { SoId = 0, SoName = "すべての営業所" }
            };
            searchOffices.AddRange(_offices);
            cmb_SearchOffice.ItemsSource = searchOffices;
            cmb_SearchOffice.SelectedIndex = 0;

            // 登録・更新用コンボ
            reg_cmb_Office.ItemsSource = _offices;
            if (_offices.Count > 0) reg_cmb_Office.SelectedIndex = 0;

            upd_cmb_Office.ItemsSource = _offices;
        }

        // =====================================================================
        // 検索機能
        // =====================================================================
        private void ExecuteSearch()
        {
            int? clId = null;
            if (int.TryParse(txt_SearchClId.Text.Trim(), out int id))
            {
                clId = id;
            }

            string name = txt_SearchClName.Text.Trim();
            string phone = txt_SearchPhone.Text.Trim();
            bool includeHidden = chk_IncludeHidden.IsChecked == true;

            int? soId = null;
            if (cmb_SearchOffice.SelectedValue is int sId && sId > 0)
            {
                soId = sId;
            }

            var results = _clientService.SearchClients(clId, name, soId, phone, includeHidden);
            _cachedList = results.Select(c => _clientService.ToDisplayModel(c)).ToList();

            dg_Customers.ItemsSource = _cachedList;
            txt_RecordCount.Text = $"表示件数: {_cachedList.Count} 件";
            UpdateActionButtonsState();
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchClId.Text = string.Empty;
            txt_SearchClName.Text = string.Empty;
            txt_SearchPhone.Text = string.Empty;
            cmb_SearchOffice.SelectedIndex = 0;
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

        private void UpdateActionButtonsState()
        {
            bool hasSelection = dg_Customers.SelectedItem is ClientDisplayModel;
            btn_EditSelected.IsEnabled = hasSelection;
            btn_HideSelected.IsEnabled = hasSelection;
        }

        private void dg_Customers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActionButtonsState();
        }

        private void dg_Customers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_Customers.SelectedItem is ClientDisplayModel selected)
            {
                OpenUpdateModal(selected.ClId);
            }
        }

        // =====================================================================
        // 新規登録
        // =====================================================================
        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
        {
            reg_txt_ClName.Text = string.Empty;
            reg_txt_ClPostal.Text = string.Empty;
            reg_txt_ClAddress.Text = string.Empty;
            reg_txt_ClPhone.Text = string.Empty;
            reg_txt_ClFax.Text = string.Empty;
            if (_offices.Count > 0) reg_cmb_Office.SelectedIndex = 0;

            modal_Register.Visibility = Visibility.Visible;
            reg_txt_ClName.Focus();
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
            string name = reg_txt_ClName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("顧客名を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_ClName.Focus();
                return;
            }

            if (reg_cmb_Office.SelectedValue is not int soId || soId <= 0)
            {
                MessageBox.Show("所属営業所を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string address = reg_txt_ClAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("住所を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_ClAddress.Focus();
                return;
            }

            string phone = reg_txt_ClPhone.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("電話番号を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_ClPhone.Focus();
                return;
            }

            var newClient = new MClient
            {
                ClName = name,
                SoId = soId,
                ClPostal = reg_txt_ClPostal.Text.Trim(),
                ClAddress = address,
                ClPhone = phone,
                ClFax = reg_txt_ClFax.Text.Trim(),
                ClFlag = 0
            };

            var (success, message, created) = _clientService.RegisterClient(newClient);
            if (!success)
            {
                MessageBox.Show(message, "登録エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"顧客「{created?.ClName}」(ID: {created?.ClId}) を登録しました。", "登録完了", MessageBoxButton.OK, MessageBoxImage.Information);
            modal_Register.Visibility = Visibility.Collapsed;
            ExecuteSearch();
        }

        // =====================================================================
        // 更新 / 編集
        // =====================================================================
        private void btn_EditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Customers.SelectedItem is ClientDisplayModel selected)
            {
                OpenUpdateModal(selected.ClId);
            }
        }

        private void OpenUpdateModal(int clId)
        {
            var client = _clientService.GetClientById(clId);
            if (client == null)
            {
                MessageBox.Show("顧客データが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            upd_txt_ClId.Text = client.ClId.ToString();
            upd_txt_ClName.Text = client.ClName ?? string.Empty;
            upd_cmb_Office.SelectedValue = client.SoId;
            upd_txt_ClPostal.Text = client.ClPostal ?? string.Empty;
            upd_txt_ClAddress.Text = client.ClAddress ?? string.Empty;
            upd_txt_ClPhone.Text = client.ClPhone ?? string.Empty;
            upd_txt_ClFax.Text = client.ClFax ?? string.Empty;

            modal_Update.Visibility = Visibility.Visible;
            upd_txt_ClName.Focus();
        }

        private void btn_CancelUpdate_Click(object sender, RoutedEventArgs e)
        {
            modal_Update.Visibility = Visibility.Collapsed;
        }

        private void modal_Update_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_Update.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(upd_txt_ClId.Text, out int clId)) return;

            string name = upd_txt_ClName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("顧客名を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                upd_txt_ClName.Focus();
                return;
            }

            if (upd_cmb_Office.SelectedValue is not int soId || soId <= 0)
            {
                MessageBox.Show("所属営業所を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string address = upd_txt_ClAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("住所を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                upd_txt_ClAddress.Focus();
                return;
            }

            string phone = upd_txt_ClPhone.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("電話番号を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                upd_txt_ClPhone.Focus();
                return;
            }

            var updatedClient = new MClient
            {
                ClId = clId,
                ClName = name,
                SoId = soId,
                ClPostal = upd_txt_ClPostal.Text.Trim(),
                ClAddress = address,
                ClPhone = phone,
                ClFax = upd_txt_ClFax.Text.Trim()
            };

            var (success, message) = _clientService.UpdateClient(updatedClient);
            if (!success)
            {
                MessageBox.Show(message, "更新エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("顧客情報を更新しました。", "更新完了", MessageBoxButton.OK, MessageBoxImage.Information);
            modal_Update.Visibility = Visibility.Collapsed;
            ExecuteSearch();
        }

        // =====================================================================
        // 非表示 (論理削除)
        // =====================================================================
        private void btn_HideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Customers.SelectedItem is ClientDisplayModel selected)
            {
                hide_txt_TargetInfo.Text = $"顧客: [{selected.ClId}] {selected.ClName}";
                hide_txt_Reason.Text = string.Empty;
                modal_Hide.Tag = selected.ClId;
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
            if (modal_Hide.Tag is not int clId) return;

            string reason = hide_txt_Reason.Text.Trim();
            var (success, message) = _clientService.SetClientHiddenFlag(clId, reason);
            if (!success)
            {
                MessageBox.Show(message, "非表示設定エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("顧客を非表示に設定しました。", "非表示完了", MessageBoxButton.OK, MessageBoxImage.Information);
            modal_Hide.Visibility = Visibility.Collapsed;
            ExecuteSearch();
        }
    }
}
