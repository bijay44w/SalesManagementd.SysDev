
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 社員管理画面
    /// 社員登録・更新・一覧・検索・非表示を管理する。
    /// </summary>
    public partial class Employee : UserControl
    {
        private readonly EmployeeService _employeeService = new();

        private List<EmployeeDisplayModel> _cachedList = new();
        private List<MSalesOffice> _offices = new();
        private List<MPosition> _positions = new();

        private int _selectedEmployeeIdForHide = 0;

        public Employee()
        {
            InitializeComponent();
            Loaded += Employee_Loaded;
        }

        // =========================================================
        // 初期表示
        // =========================================================

        private void Employee_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMasterData();
            ExecuteSearch();
            UpdateActionButtons();
        }

        /// <summary>
        /// 営業所・役職マスタを読み込む
        /// </summary>
        private void LoadMasterData()
        {
            try
            {
                _offices = _employeeService.GetSalesOffices();
                _positions = _employeeService.GetPositions();

                cmb_SearchOffice.ItemsSource = _offices;
                cmb_SearchPosition.ItemsSource = _positions;

                reg_cmb_Office.ItemsSource = _offices;
                reg_cmb_Position.ItemsSource = _positions;

                upd_cmb_Office.ItemsSource = _offices;
                upd_cmb_Position.ItemsSource = _positions;

                cmb_SearchOffice.SelectedIndex = -1;
                cmb_SearchPosition.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("マスタ情報の読み込みに失敗しました。", ex);
            }
        }

        // =========================================================
        // 一覧・検索
        // =========================================================

        /// <summary>
        /// 検索条件に基づいて社員一覧を取得する
        /// </summary>
        private void ExecuteSearch()
        {
            try
            {
                int? employeeId = null;

                if (!string.IsNullOrWhiteSpace(txt_SearchEmId.Text))
                {
                    if (!int.TryParse(txt_SearchEmId.Text, out int id)
                        || id <= 0)
                    {
                        ShowMessage("社員IDは正しい数値で入力してください。");
                        return;
                    }

                    employeeId = id;
                }

                int? officeId = null;
                if (cmb_SearchOffice.SelectedValue != null)
                {
                    officeId = Convert.ToInt32(
                        cmb_SearchOffice.SelectedValue);
                }

                int? positionId = null;
                if (cmb_SearchPosition.SelectedValue != null)
                {
                    positionId = Convert.ToInt32(
                        cmb_SearchPosition.SelectedValue);
                }

                bool includeHidden =
                    chk_IncludeHidden.IsChecked == true;

                _cachedList = _employeeService.SearchEmployees(
                    emId: employeeId,
                    name: txt_SearchEmName.Text,
                    soId: officeId,
                    poId: positionId,
                    phone: txt_SearchPhone.Text,
                    includeHidden: includeHidden
                );

                dg_Employees.ItemsSource = null;
                dg_Employees.ItemsSource = _cachedList;

                txt_RecordCount.Text =
                    $"表示件数: {_cachedList.Count} 件";

                UpdateActionButtons();
            }
            catch (Exception ex)
            {
                ShowError("社員検索に失敗しました。", ex);
            }
        }

        private void SearchField_KeyDown(
            object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteSearch();
            }
        }

        // 過去のXAMLから参照されている場合にも対応
        private void txt_SearchEmName_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            // 入力中は検索せず、検索ボタンまたはEnterで検索する。
        }

        private void cmb_Filter_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ExecuteSearch();
        }

        private void chk_IncludeHidden_Click(
            object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void btn_Search_Click(
            object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void btn_Clear_Click(
            object sender, RoutedEventArgs e)
        {
            txt_SearchEmId.Clear();
            txt_SearchEmName.Clear();
            txt_SearchPhone.Clear();

            cmb_SearchOffice.SelectedIndex = -1;
            cmb_SearchPosition.SelectedIndex = -1;

            chk_IncludeHidden.IsChecked = false;

            ExecuteSearch();
        }

        // =========================================================
        // 一覧選択
        // =========================================================

        private void dg_Employees_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            UpdateActionButtons();
        }

        private void dg_Employees_MouseDoubleClick(
            object sender, MouseButtonEventArgs e)
        {
            if (dg_Employees.SelectedItem is EmployeeDisplayModel employee)
            {
                OpenUpdateModal(employee);
            }
        }

        private void UpdateActionButtons()
        {
            bool selected =
                dg_Employees.SelectedItem is EmployeeDisplayModel;

            btn_EditSelected.IsEnabled = selected;
            btn_HideSelected.IsEnabled = selected;
        }

        // =========================================================
        // 新規社員登録
        // =========================================================

        private void btn_NewRegister_Click(
            object sender, RoutedEventArgs e)
        {
            OpenRegisterModal();
        }

        private void OpenRegisterModal()
        {
            reg_txt_EmName.Clear();
            reg_cmb_Office.SelectedIndex = -1;
            reg_cmb_Position.SelectedIndex = -1;

            reg_dp_Hiredate.SelectedDate = DateTime.Today;

            reg_txt_Phone.Clear();
            reg_txt_Password.Text = "1234";

            modal_Register.Visibility = Visibility.Visible;
        }

        private void modal_Register_Backdrop_Click(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                CloseRegisterModal();
            }
        }

        private void btn_CancelRegister_Click(
            object sender, RoutedEventArgs e)
        {
            CloseRegisterModal();
        }

        private void CloseRegisterModal()
        {
            modal_Register.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteRegister_Click(
            object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(reg_txt_EmName.Text))
            {
                ShowMessage("社員名を入力してください。");
                return;
            }

            if (reg_cmb_Office.SelectedValue == null)
            {
                ShowMessage("所属営業所を選択してください。");
                return;
            }

            if (reg_cmb_Position.SelectedValue == null)
            {
                ShowMessage("役職を選択してください。");
                return;
            }

            if (string.IsNullOrWhiteSpace(reg_txt_Phone.Text))
            {
                ShowMessage("電話番号を入力してください。");
                return;
            }

            string password = reg_txt_Password.Text;

            if (string.IsNullOrWhiteSpace(password)
                || password.Length < 4
                || password.Length > 10)
            {
                ShowMessage("パスワードは4～10文字で入力してください。");
                return;
            }

            var employee = new MEmployee
            {
                EmName = reg_txt_EmName.Text.Trim(),

                SoId = Convert.ToInt32(
                    reg_cmb_Office.SelectedValue),

                PoId = Convert.ToInt32(
                    reg_cmb_Position.SelectedValue),

                EmHiredate =
                    reg_dp_Hiredate.SelectedDate ?? DateTime.Today,

                EmPhone = reg_txt_Phone.Text.Trim(),

                EmPassword = password,

                EmFlag = 0
            };

            var result =
                _employeeService.RegisterEmployee(employee);

            if (!result.Success)
            {
                ShowMessage(result.Message);
                return;
            }

            ShowMessage(result.Message);

            CloseRegisterModal();
            ExecuteSearch();
        }

        // =========================================================
        // 社員情報更新
        // =========================================================

        private void btn_EditSelected_Click(
            object sender, RoutedEventArgs e)
        {
            if (dg_Employees.SelectedItem is EmployeeDisplayModel employee)
            {
                OpenUpdateModal(employee);
            }
            else
            {
                ShowMessage("更新する社員を選択してください。");
            }
        }

        private void OpenUpdateModal(EmployeeDisplayModel employee)
        {
            upd_txt_EmId.Text = employee.EmId.ToString();
            upd_txt_EmName.Text = employee.EmName;

            upd_cmb_Office.SelectedValue = employee.SoId;
            upd_cmb_Position.SelectedValue = employee.PoId;

            upd_dp_Hiredate.SelectedDate = employee.EmHiredate;

            upd_txt_Phone.Text = employee.EmPhone;

            // パスワードは変更する場合のみ入力
            upd_txt_Password.Clear();

            modal_Update.Visibility = Visibility.Visible;
        }

        private void modal_Update_Backdrop_Click(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                CloseUpdateModal();
            }
        }

        private void btn_CancelUpdate_Click(
            object sender, RoutedEventArgs e)
        {
            CloseUpdateModal();
        }

        private void CloseUpdateModal()
        {
            modal_Update.Visibility = Visibility.Collapsed;
        }

        private void btn_ExecuteUpdate_Click(
            object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(upd_txt_EmId.Text, out int employeeId)
                || employeeId <= 0)
            {
                ShowMessage("社員IDが正しくありません。");
                return;
            }

            if (string.IsNullOrWhiteSpace(upd_txt_EmName.Text))
            {
                ShowMessage("社員名を入力してください。");
                return;
            }

            if (upd_cmb_Office.SelectedValue == null)
            {
                ShowMessage("所属営業所を選択してください。");
                return;
            }

            if (upd_cmb_Position.SelectedValue == null)
            {
                ShowMessage("役職を選択してください。");
                return;
            }

            if (string.IsNullOrWhiteSpace(upd_txt_Phone.Text))
            {
                ShowMessage("電話番号を入力してください。");
                return;
            }

            string password = upd_txt_Password.Text;

            bool updatePassword =
                !string.IsNullOrWhiteSpace(password);

            if (updatePassword
                && (password.Length < 4 || password.Length > 10))
            {
                ShowMessage("パスワードは4～10文字で入力してください。");
                return;
            }

            var employee = new MEmployee
            {
                EmId = employeeId,

                EmName = upd_txt_EmName.Text.Trim(),

                SoId = Convert.ToInt32(
                    upd_cmb_Office.SelectedValue),

                PoId = Convert.ToInt32(
                    upd_cmb_Position.SelectedValue),

                EmHiredate =
                    upd_dp_Hiredate.SelectedDate ?? DateTime.Today,

                EmPhone = upd_txt_Phone.Text.Trim(),

                EmPassword = password
            };

            var result = _employeeService.UpdateEmployee(
                employee,
                updatePassword
            );

            if (!result.Success)
            {
                ShowMessage(result.Message);
                return;
            }

            ShowMessage(result.Message);

            CloseUpdateModal();
            ExecuteSearch();
        }

        // =========================================================
        // 社員非表示
        // =========================================================

        private void btn_HideSelected_Click(
            object sender, RoutedEventArgs e)
        {
            if (dg_Employees.SelectedItem is EmployeeDisplayModel employee)
            {
                OpenHideModal(employee);
            }
            else
            {
                ShowMessage("非表示にする社員を選択してください。");
            }
        }

        private void OpenHideModal(EmployeeDisplayModel employee)
        {
            _selectedEmployeeIdForHide = employee.EmId;

            hide_txt_TargetInfo.Text =
                $"社員ID: {employee.EmId} / 社員名: {employee.EmName}";

            hide_txt_Reason.Text = "退職による非表示設定";

            modal_Hide.Visibility = Visibility.Visible;
        }

        private void modal_Hide_Backdrop_Click(
            object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                CloseHideModal();
            }
        }

        private void btn_CancelHide_Click(
            object sender, RoutedEventArgs e)
        {
            CloseHideModal();
        }

        private void CloseHideModal()
        {
            modal_Hide.Visibility = Visibility.Collapsed;
            _selectedEmployeeIdForHide = 0;
        }

        private void btn_ExecuteHide_Click(
            object sender, RoutedEventArgs e)
        {
            if (_selectedEmployeeIdForHide <= 0)
            {
                ShowMessage("非表示対象の社員を選択してください。");
                return;
            }

            var answer = MessageBox.Show(
                "この社員を非表示にしますか？",
                "社員非表示確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (answer != MessageBoxResult.Yes)
                return;

            var result = _employeeService.HideEmployee(
                _selectedEmployeeIdForHide,
                hide_txt_Reason.Text
            );

            if (!result.Success)
            {
                ShowMessage(result.Message);
                return;
            }

            ShowMessage(result.Message);

            CloseHideModal();
            ExecuteSearch();
        }

        // =========================================================
        // 共通メッセージ
        // =========================================================

        private void ShowMessage(string message)
        {
            MessageBox.Show(
                message,
                "社員管理",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show(
                message + "\n" + ex.Message,
                "社員管理エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
