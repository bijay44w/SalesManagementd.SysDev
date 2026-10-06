using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class F_Home : Window
    {
        private readonly MEmployee _loggedInEmployee;
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
    }
}
