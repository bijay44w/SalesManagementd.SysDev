using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

 // test 

namespace SalesManagement_SysDev
{
    public partial class F_Home : Window
    {
        private readonly MEmployee _loggedInEmployee;
        private readonly ClientService _clientService = new ClientService();
        private List<ClientDisplayModel> _cachedClientList = new List<ClientDisplayModel>();

        private bool _isCurrentRevealed = false;
        private bool _isNewRevealed = false;
        private bool _isConfirmRevealed = false;

        public F_Home() : this(null!)
        {
        }

        public F_Home(MEmployee employee)
        {
            InitializeComponent();
            _loggedInEmployee = employee;

            LoadEmployeeInfo();
        }

        private void LoadEmployeeInfo()
        {
            if (_loggedInEmployee != null)
            {
                txt_UserDisplay.Text = $"{_loggedInEmployee.EmName} 様";　
                txt_Welcome.Text = $"ようこそ、{_loggedInEmployee.EmName} 様";

                string officeName = _loggedInEmployee.So?.SoName ?? $"営業所 #{_loggedInEmployee.SoId}";
                string positionName = _loggedInEmployee.Po?.PoName ?? $"役職 #{_loggedInEmployee.PoId}";

                txt_UserDetail.Text = $"所属: {officeName} ｜ 役職: {positionName} ｜ 社員ID: {_loggedInEmployee.EmId}";
            }
        }

        #region User Profile & Password Change Modal

        private void border_UserPill_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenUserProfileModal();
        }

        private void OpenUserProfileModal()
        {
            if (_loggedInEmployee != null)
            {
                modal_txt_EmpId.Text = _loggedInEmployee.EmId.ToString();
                modal_txt_EmpName.Text = $"{_loggedInEmployee.EmName} 様";
                modal_txt_Office.Text = _loggedInEmployee.So?.SoName ?? $"営業所 #{_loggedInEmployee.SoId}";
                modal_txt_Position.Text = _loggedInEmployee.Po?.PoName ?? $"役職 #{_loggedInEmployee.PoId}";
            }

            // Reset password fields and alerts
            txt_CurrentPassword.Password = "";
            txt_CurrentPasswordRevealed.Text = "";
            txt_CurrentPasswordRevealed.Visibility = Visibility.Collapsed;
            txt_CurrentPassword.Visibility = Visibility.Visible;
            _isCurrentRevealed = false;

            txt_ModalNewPassword.Password = "";
            txt_ModalNewPasswordRevealed.Text = "";
            txt_ModalNewPasswordRevealed.Visibility = Visibility.Collapsed;
            txt_ModalNewPassword.Visibility = Visibility.Visible;
            _isNewRevealed = false;

            txt_ModalConfirmPassword.Password = "";
            txt_ModalConfirmPasswordRevealed.Text = "";
            txt_ModalConfirmPasswordRevealed.Visibility = Visibility.Collapsed;
            txt_ModalConfirmPassword.Visibility = Visibility.Visible;
            _isConfirmRevealed = false;

            border_ModalErrorAlert.Visibility = Visibility.Collapsed;
            border_ModalSuccessAlert.Visibility = Visibility.Collapsed;

            modal_UserProfile.Visibility = Visibility.Visible;
            txt_CurrentPassword.Focus();
        }

        private void btn_CloseProfileModal_Click(object sender, RoutedEventArgs e)
        {
            modal_UserProfile.Visibility = Visibility.Collapsed;
        }

        private void modal_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_UserProfile.Visibility = Visibility.Collapsed;
        }

        private void btn_ToggleCurrentPassword_Click(object sender, RoutedEventArgs e)
        {
            _isCurrentRevealed = !_isCurrentRevealed;
            if (_isCurrentRevealed)
            {
                txt_CurrentPasswordRevealed.Text = txt_CurrentPassword.Password;
                txt_CurrentPassword.Visibility = Visibility.Collapsed;
                txt_CurrentPasswordRevealed.Visibility = Visibility.Visible;
                txt_CurrentPasswordRevealed.Focus();
                txt_CurrentPasswordRevealed.CaretIndex = txt_CurrentPasswordRevealed.Text.Length;
            }
            else
            {
                txt_CurrentPassword.Password = txt_CurrentPasswordRevealed.Text;
                txt_CurrentPasswordRevealed.Visibility = Visibility.Collapsed;
                txt_CurrentPassword.Visibility = Visibility.Visible;
                txt_CurrentPassword.Focus();
            }
        }

        private void btn_ToggleModalNewPassword_Click(object sender, RoutedEventArgs e)
        {
            _isNewRevealed = !_isNewRevealed;
            if (_isNewRevealed)
            {
                txt_ModalNewPasswordRevealed.Text = txt_ModalNewPassword.Password;
                txt_ModalNewPassword.Visibility = Visibility.Collapsed;
                txt_ModalNewPasswordRevealed.Visibility = Visibility.Visible;
                txt_ModalNewPasswordRevealed.Focus();
                txt_ModalNewPasswordRevealed.CaretIndex = txt_ModalNewPasswordRevealed.Text.Length;
            }
            else
            {
                txt_ModalNewPassword.Password = txt_ModalNewPasswordRevealed.Text;
                txt_ModalNewPasswordRevealed.Visibility = Visibility.Collapsed;
                txt_ModalNewPassword.Visibility = Visibility.Visible;
                txt_ModalNewPassword.Focus();
            }
        }

        private void btn_ToggleModalConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmRevealed = !_isConfirmRevealed;
            if (_isConfirmRevealed)
            {
                txt_ModalConfirmPasswordRevealed.Text = txt_ModalConfirmPassword.Password;
                txt_ModalConfirmPassword.Visibility = Visibility.Collapsed;
                txt_ModalConfirmPasswordRevealed.Visibility = Visibility.Visible;
                txt_ModalConfirmPasswordRevealed.Focus();
                txt_ModalConfirmPasswordRevealed.CaretIndex = txt_ModalConfirmPasswordRevealed.Text.Length;
            }
            else
            {
                txt_ModalConfirmPassword.Password = txt_ModalConfirmPasswordRevealed.Text;
                txt_ModalConfirmPasswordRevealed.Visibility = Visibility.Collapsed;
                txt_ModalConfirmPassword.Visibility = Visibility.Visible;
                txt_ModalConfirmPassword.Focus();
            }
        }

        private void btn_SubmitPasswordChange_Click(object sender, RoutedEventArgs e)
        {
            border_ModalErrorAlert.Visibility = Visibility.Collapsed;
            border_ModalSuccessAlert.Visibility = Visibility.Collapsed;

            string curPass = _isCurrentRevealed ? txt_CurrentPasswordRevealed.Text : txt_CurrentPassword.Password;
            string newPass = _isNewRevealed ? txt_ModalNewPasswordRevealed.Text : txt_ModalNewPassword.Password;
            string confPass = _isConfirmRevealed ? txt_ModalConfirmPasswordRevealed.Text : txt_ModalConfirmPassword.Password;

            // 1. Validation
            if (string.IsNullOrEmpty(curPass))
            {
                ShowModalError("現在のパスワードを入力してください。");
                if (_isCurrentRevealed) txt_CurrentPasswordRevealed.Focus(); else txt_CurrentPassword.Focus();
                return;
            }

            if (curPass != _loggedInEmployee.EmPassword)
            {
                ShowModalError("現在のパスワードが正しくありません。");
                if (_isCurrentRevealed) txt_CurrentPasswordRevealed.Focus(); else txt_CurrentPassword.Focus();
                return;
            }

            if (string.IsNullOrEmpty(newPass))
            {
                ShowModalError("新しいパスワードを入力してください。");
                if (_isNewRevealed) txt_ModalNewPasswordRevealed.Focus(); else txt_ModalNewPassword.Focus();
                return;
            }

            if (newPass == curPass)
            {
                ShowModalError("新しいパスワードは現在のパスワードと異なるものを入力してください。");
                if (_isNewRevealed) txt_ModalNewPasswordRevealed.Focus(); else txt_ModalNewPassword.Focus();
                return;
            }

            if (newPass != confPass)
            {
                ShowModalError("新しいパスワードと確認用パスワードが一致しません。");
                if (_isConfirmRevealed) txt_ModalConfirmPasswordRevealed.Focus(); else txt_ModalConfirmPassword.Focus();
                return;
            }

            // 2. Database Update
            try
            {
                using var context = new SalesManagementContext();
                var emp = context.MEmployees.FirstOrDefault(x => x.EmId == _loggedInEmployee.EmId);
                if (emp != null)
                {
                    emp.EmPassword = newPass;
                    context.SaveChanges();

                    // Update in-memory session password
                    _loggedInEmployee.EmPassword = newPass;

                    ShowModalSuccess("パスワードを正常に変更しました。");

                    // Clear fields
                    txt_CurrentPassword.Password = "";
                    txt_CurrentPasswordRevealed.Text = "";
                    txt_ModalNewPassword.Password = "";
                    txt_ModalNewPasswordRevealed.Text = "";
                    txt_ModalConfirmPassword.Password = "";
                    txt_ModalConfirmPasswordRevealed.Text = "";
                }
                else
                {
                    ShowModalError("社員情報の取得に失敗しました。");
                }
            }
            catch (Exception ex)
            {
                ShowModalError($"パスワード更新中にエラーが発生しました: {ex.Message}");
            }
        }

        private void ShowModalError(string message)
        {
            txt_ModalErrorMessage.Text = message;
            border_ModalErrorAlert.Visibility = Visibility.Visible;
        }

        private void ShowModalSuccess(string message)
        {
            txt_ModalSuccessMessage.Text = message;
            border_ModalSuccessAlert.Visibility = Visibility.Visible;
        }

        #endregion

        private void btn_Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("ログアウトしてログイン画面に戻りますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new F_login();
                loginWindow.Show();
                this.Close();
            }
        }

        #region Window Chrome Button Handlers

        private void btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_Maximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region 3. 顧客管理 (Customer Management) Operations & Modals

        // ---------------------------------------------------------------------
        // 3.3 顧客一覧表示 (List All Clients)
        // ---------------------------------------------------------------------
        private void card_ClientList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenClientListModal();
        }

        private void OpenClientListModal()
        {
            txt_ListFilterKeyword.Text = "";
            chk_ListIncludeHidden.IsChecked = true;
            modal_ClientList.Visibility = Visibility.Visible;
            LoadClientList();
        }

        private void LoadClientList()
        {
            bool includeHidden = chk_ListIncludeHidden.IsChecked == true;
            var clients = _clientService.GetAllClients(includeHidden);
            _cachedClientList = clients.Select(c => ToDisplayModel(c)).ToList();
            ApplyClientListFilter();
        }

        private void ApplyClientListFilter()
        {
            string keyword = (txt_ListFilterKeyword.Text ?? "").Trim();
            var filtered = _cachedClientList;
            if (!string.IsNullOrEmpty(keyword))
            {
                filtered = filtered.Where(c =>
                    c.ClName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    c.ClId.ToString().Contains(keyword) ||
                    c.ClPhone.Contains(keyword) ||
                    c.ClAddress.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    c.SoName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            dg_ClientList.ItemsSource = filtered;
            txt_ListCount.Text = $"表示件数: {filtered.Count} 件 (全 {_cachedClientList.Count} 件中)";
        }

        private void txt_ListFilterKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            ApplyClientListFilter();
        }

        private void chk_ListIncludeHidden_Click(object sender, RoutedEventArgs e)
        {
            LoadClientList();
        }

        private void btn_ListRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadClientList();
        }

        private void btn_CloseClientList_Click(object sender, RoutedEventArgs e)
        {
            modal_ClientList.Visibility = Visibility.Collapsed;
        }

        private void modal_ClientList_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ClientList.Visibility = Visibility.Collapsed;
        }

        private void dg_ClientList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_ClientList.SelectedItem is ClientDisplayModel selected)
            {
                OpenClientUpdateModal(selected.ClId);
            }
        }

        private void btn_ListEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_ClientList.SelectedItem is ClientDisplayModel selected)
            {
                modal_ClientList.Visibility = Visibility.Collapsed;
                OpenClientUpdateModal(selected.ClId);
            }
            else
            {
                MessageBox.Show("一覧から編集する顧客を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btn_ListHideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_ClientList.SelectedItem is ClientDisplayModel selected)
            {
                modal_ClientList.Visibility = Visibility.Collapsed;
                OpenClientHideModal(selected.ClId);
            }
            else
            {
                MessageBox.Show("一覧から非表示にする顧客を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ---------------------------------------------------------------------
        // 3.1 顧客登録 (Register Client)
        // ---------------------------------------------------------------------
        private void card_ClientRegister_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenClientRegisterModal();
        }

        private void OpenClientRegisterModal()
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            // Load sales offices
            var offices = _clientService.GetSalesOffices();
            reg_cmb_SalesOffice.ItemsSource = offices;
            if (offices.Count > 0)
            {
                reg_cmb_SalesOffice.SelectedIndex = 0;
            }

            reg_txt_ClName.Text = "";
            reg_txt_ClPostal.Text = "";
            reg_txt_ClAddress.Text = "";
            reg_txt_ClPhone.Text = "";
            reg_txt_ClFax.Text = "";

            modal_ClientRegister.Visibility = Visibility.Visible;
            reg_txt_ClName.Focus();
        }

        private void btn_CloseClientRegister_Click(object sender, RoutedEventArgs e)
        {
            modal_ClientRegister.Visibility = Visibility.Collapsed;
        }

        private void modal_ClientRegister_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ClientRegister.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteClientRegister_Click(object sender, RoutedEventArgs e)
        {
            border_RegErrorAlert.Visibility = Visibility.Collapsed;
            border_RegSuccessAlert.Visibility = Visibility.Collapsed;

            if (reg_cmb_SalesOffice.SelectedValue == null)
            {
                ShowRegError("所属営業所を選択してください。");
                return;
            }

            var newClient = new MClient
            {
                ClName = reg_txt_ClName.Text.Trim(),
                SoId = (int)reg_cmb_SalesOffice.SelectedValue,
                ClPostal = reg_txt_ClPostal.Text.Trim(),
                ClAddress = reg_txt_ClAddress.Text.Trim(),
                ClPhone = reg_txt_ClPhone.Text.Trim(),
                ClFax = reg_txt_ClFax.Text.Trim()
            };

            var result = _clientService.RegisterClient(newClient);
            if (result.Success)
            {
                ShowRegSuccess(result.Message);
                // Clear inputs
                reg_txt_ClName.Text = "";
                reg_txt_ClPostal.Text = "";
                reg_txt_ClAddress.Text = "";
                reg_txt_ClPhone.Text = "";
                reg_txt_ClFax.Text = "";
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

        // ---------------------------------------------------------------------
        // 3.2 顧客更新 (Update Client)
        // ---------------------------------------------------------------------
        private void card_ClientUpdate_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenClientUpdateModal();
        }

        private void OpenClientUpdateModal(int? targetClId = null)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            var offices = _clientService.GetSalesOffices();
            upd_cmb_SalesOffice.ItemsSource = offices;

            upd_txt_SearchClId.Text = targetClId.HasValue ? targetClId.Value.ToString() : "";
            ClearUpdateFormFields();

            modal_ClientUpdate.Visibility = Visibility.Visible;

            if (targetClId.HasValue && targetClId.Value > 0)
            {
                LoadClientForUpdate(targetClId.Value);
            }
            else
            {
                upd_txt_SearchClId.Focus();
            }
        }

        private void ClearUpdateFormFields()
        {
            upd_txt_ClName.Text = "";
            upd_txt_ClPostal.Text = "";
            upd_txt_ClAddress.Text = "";
            upd_txt_ClPhone.Text = "";
            upd_txt_ClFax.Text = "";
            upd_cmb_ClFlag.SelectedIndex = 0;
            upd_txt_ClHidden.Text = "";
        }

        private void upd_txt_SearchClId_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_LoadClientForUpdate_Click(sender, e);
            }
        }

        private void btn_LoadClientForUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchClId.Text.Trim(), out int clId) || clId <= 0)
            {
                ShowUpdError("有効な顧客IDを半角数値で入力してください。");
                return;
            }

            LoadClientForUpdate(clId);
        }

        private void LoadClientForUpdate(int clId)
        {
            var client = _clientService.GetClientById(clId);
            if (client == null)
            {
                ShowUpdError($"顧客ID: {clId} のデータは見つかりませんでした。");
                ClearUpdateFormFields();
                return;
            }

            upd_txt_SearchClId.Text = client.ClId.ToString();
            upd_txt_ClName.Text = client.ClName;
            upd_cmb_SalesOffice.SelectedValue = client.SoId;
            upd_txt_ClPostal.Text = client.ClPostal;
            upd_txt_ClAddress.Text = client.ClAddress;
            upd_txt_ClPhone.Text = client.ClPhone;
            upd_txt_ClFax.Text = client.ClFax;
            upd_cmb_ClFlag.SelectedIndex = client.ClFlag == 2 ? 1 : 0;
            upd_txt_ClHidden.Text = client.ClHidden ?? "";
        }

        private void btn_ExecuteClientUpdate_Click(object sender, RoutedEventArgs e)
        {
            border_UpdErrorAlert.Visibility = Visibility.Collapsed;
            border_UpdSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(upd_txt_SearchClId.Text.Trim(), out int clId) || clId <= 0)
            {
                ShowUpdError("更新対象の顧客IDを入力して情報を呼び出してください。");
                return;
            }

            if (upd_cmb_SalesOffice.SelectedValue == null)
            {
                ShowUpdError("所属営業所を選択してください。");
                return;
            }

            int flag = upd_cmb_ClFlag.SelectedIndex == 1 ? 2 : 0;

            var updatedObj = new MClient
            {
                ClId = clId,
                ClName = upd_txt_ClName.Text.Trim(),
                SoId = (int)upd_cmb_SalesOffice.SelectedValue,
                ClPostal = upd_txt_ClPostal.Text.Trim(),
                ClAddress = upd_txt_ClAddress.Text.Trim(),
                ClPhone = upd_txt_ClPhone.Text.Trim(),
                ClFax = upd_txt_ClFax.Text.Trim(),
                ClFlag = flag,
                ClHidden = upd_txt_ClHidden.Text.Trim()
            };

            var result = _clientService.UpdateClient(updatedObj);
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

        private void btn_CloseClientUpdate_Click(object sender, RoutedEventArgs e)
        {
            modal_ClientUpdate.Visibility = Visibility.Collapsed;
        }

        private void modal_ClientUpdate_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ClientUpdate.Visibility = Visibility.Collapsed;
        }

        // ---------------------------------------------------------------------
        // 3.4 顧客検索 (Search Clients)
        // ---------------------------------------------------------------------
        private void card_ClientSearch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenClientSearchModal();
        }

        private void OpenClientSearchModal()
        {
            var offices = _clientService.GetSalesOffices();
            var officeOptions = new List<MSalesOffice> { new MSalesOffice { SoId = 0, SoName = "すべての営業所" } };
            officeOptions.AddRange(offices);
            srch_cmb_SalesOffice.ItemsSource = officeOptions;
            srch_cmb_SalesOffice.SelectedIndex = 0;

            srch_txt_ClId.Text = "";
            srch_txt_ClName.Text = "";
            srch_txt_ClPhone.Text = "";

            modal_ClientSearch.Visibility = Visibility.Visible;
            ExecuteClientSearch();
        }

        private void btn_ExecuteClientSearch_Click(object sender, RoutedEventArgs e)
        {
            ExecuteClientSearch();
        }

        private void ExecuteClientSearch()
        {
            int? clId = null;
            if (int.TryParse(srch_txt_ClId.Text.Trim(), out int parsedId) && parsedId > 0)
            {
                clId = parsedId;
            }

            string? name = string.IsNullOrWhiteSpace(srch_txt_ClName.Text) ? null : srch_txt_ClName.Text.Trim();
            int? soId = null;
            if (srch_cmb_SalesOffice.SelectedValue is int selectedSo && selectedSo > 0)
            {
                soId = selectedSo;
            }
            string? phone = string.IsNullOrWhiteSpace(srch_txt_ClPhone.Text) ? null : srch_txt_ClPhone.Text.Trim();

            var results = _clientService.SearchClients(clId, name, soId, phone, includeHidden: true);
            var displayList = results.Select(c => ToDisplayModel(c)).ToList();
            dg_ClientSearchResults.ItemsSource = displayList;
            srch_txt_Count.Text = $"抽出件数: {displayList.Count} 件";
        }

        private void btn_ClearClientSearch_Click(object sender, RoutedEventArgs e)
        {
            srch_txt_ClId.Text = "";
            srch_txt_ClName.Text = "";
            srch_cmb_SalesOffice.SelectedIndex = 0;
            srch_txt_ClPhone.Text = "";
            ExecuteClientSearch();
        }

        private void btn_CloseClientSearch_Click(object sender, RoutedEventArgs e)
        {
            modal_ClientSearch.Visibility = Visibility.Collapsed;
        }

        private void modal_ClientSearch_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ClientSearch.Visibility = Visibility.Collapsed;
        }

        private void dg_ClientSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_ClientSearchResults.SelectedItem is ClientDisplayModel selected)
            {
                modal_ClientSearch.Visibility = Visibility.Collapsed;
                OpenClientUpdateModal(selected.ClId);
            }
        }

        private void btn_SearchEditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_ClientSearchResults.SelectedItem is ClientDisplayModel selected)
            {
                modal_ClientSearch.Visibility = Visibility.Collapsed;
                OpenClientUpdateModal(selected.ClId);
            }
            else
            {
                MessageBox.Show("一覧から編集する顧客を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ---------------------------------------------------------------------
        // 3.5 顧客非表示 (フラグ更新 0 -> 2)
        // ---------------------------------------------------------------------
        private void card_ClientHide_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenClientHideModal();
        }

        private void OpenClientHideModal(int? targetClId = null)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            hide_txt_ClId.Text = targetClId.HasValue ? targetClId.Value.ToString() : "";
            hide_txt_DisplayClName.Text = "-";
            hide_txt_DisplayOffice.Text = "-";
            hide_txt_DisplayFlag.Text = "-";
            hide_txt_Reason.Text = "";

            modal_ClientHide.Visibility = Visibility.Visible;

            if (targetClId.HasValue && targetClId.Value > 0)
            {
                LoadClientForHide(targetClId.Value);
            }
            else
            {
                hide_txt_ClId.Focus();
            }
        }

        private void hide_txt_ClId_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_LoadClientForHide_Click(sender, e);
            }
        }

        private void btn_LoadClientForHide_Click(object sender, RoutedEventArgs e)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(hide_txt_ClId.Text.Trim(), out int clId) || clId <= 0)
            {
                ShowHideError("有効な顧客IDを入力してください。");
                return;
            }

            LoadClientForHide(clId);
        }

        private void LoadClientForHide(int clId)
        {
            var client = _clientService.GetClientById(clId);
            if (client == null)
            {
                ShowHideError($"顧客ID: {clId} のデータが見つかりません。");
                hide_txt_DisplayClName.Text = "-";
                hide_txt_DisplayOffice.Text = "-";
                hide_txt_DisplayFlag.Text = "-";
                return;
            }

            hide_txt_ClId.Text = client.ClId.ToString();
            hide_txt_DisplayClName.Text = client.ClName;
            hide_txt_DisplayOffice.Text = client.So?.SoName ?? $"営業所 #{client.SoId}";
            hide_txt_DisplayFlag.Text = client.ClFlag == 2 ? "2 (すでに非表示)" : "0 (通常表示)";
            hide_txt_Reason.Text = client.ClHidden ?? "";
        }

        private void btn_ExecuteClientHide_Click(object sender, RoutedEventArgs e)
        {
            border_HideErrorAlert.Visibility = Visibility.Collapsed;
            border_HideSuccessAlert.Visibility = Visibility.Collapsed;

            if (!int.TryParse(hide_txt_ClId.Text.Trim(), out int clId) || clId <= 0)
            {
                ShowHideError("対象の顧客IDを入力してください。");
                return;
            }

            var confirm = MessageBox.Show($"顧客ID: {clId} の管理フラグを 2 (非表示) に更新しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            string reason = hide_txt_Reason.Text.Trim();
            var result = _clientService.SetClientHiddenFlag(clId, string.IsNullOrEmpty(reason) ? "管理者による非表示設定" : reason);

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

        private void btn_CloseClientHide_Click(object sender, RoutedEventArgs e)
        {
            modal_ClientHide.Visibility = Visibility.Collapsed;
        }

        private void modal_ClientHide_Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            modal_ClientHide.Visibility = Visibility.Collapsed;
        }

        // ---------------------------------------------------------------------
        // Helper Display Model Mapper
        // ---------------------------------------------------------------------
        private static ClientDisplayModel ToDisplayModel(MClient c)
        {
            return new ClientDisplayModel
            {
                ClId = c.ClId,
                ClName = c.ClName ?? "",
                SoId = c.SoId,
                SoName = c.So?.SoName ?? $"営業所 #{c.SoId}",
                ClPostal = c.ClPostal ?? "",
                ClAddress = c.ClAddress ?? "",
                ClPhone = c.ClPhone ?? "",
                ClFax = c.ClFax ?? "",
                ClFlag = c.ClFlag,
                ClHidden = c.ClHidden ?? ""
            };
        }

        #endregion
    }

    /// <summary>
    /// UI binding model for client DataGrid views
    /// </summary>
    public class ClientDisplayModel
    {
        public int ClId { get; set; }
        public string ClName { get; set; } = "";
        public int SoId { get; set; }
        public string SoName { get; set; } = "";
        public string ClPostal { get; set; } = "";
        public string ClAddress { get; set; } = "";
        public string ClPhone { get; set; } = "";
        public string ClFax { get; set; } = "";
        public int ClFlag { get; set; }
        public string FlagDisplay => ClFlag == 2 ? "非表示 (2)" : "通常 (0)";
        public string ClHidden { get; set; } = "";
    }
}
