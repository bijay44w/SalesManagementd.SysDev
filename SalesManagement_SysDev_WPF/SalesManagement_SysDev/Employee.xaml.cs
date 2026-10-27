using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
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

        private void Employee_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMasters();
            ExecuteSearch();
            UpdateActionButtonsState();
        }

        private void LoadMasters()
        {
            _offices = _employeeService.GetSalesOffices();
            _positions = _employeeService.GetPositions();

            // 検索コンボ用 (先頭に「すべて」を追加)
            var searchOffices = new List<MSalesOffice>
            {
                new MSalesOffice { SoId = 0, SoName = "すべての営業所" }
            };
            searchOffices.AddRange(_offices);
            cmb_SearchOffice.ItemsSource = searchOffices;
            cmb_SearchOffice.SelectedIndex = 0;

            var searchPositions = new List<MPosition>
            {
                new MPosition { PoId = 0, PoName = "すべての役職" }
            };
            searchPositions.AddRange(_positions);
            cmb_SearchPosition.ItemsSource = searchPositions;
            cmb_SearchPosition.SelectedIndex = 0;

            // 登録・更新用コンボ
            reg_cmb_Office.ItemsSource = _offices;
            if (_offices.Count > 0) reg_cmb_Office.SelectedIndex = 0;

            reg_cmb_Position.ItemsSource = _positions;
            if (_positions.Count > 0) reg_cmb_Position.SelectedIndex = 0;

            upd_cmb_Office.ItemsSource = _offices;
            upd_cmb_Position.ItemsSource = _positions;
        }

        // =====================================================================
        // 検索機能
        // =====================================================================
        private void ExecuteSearch()
        {
            int? emId = null;
            if (int.TryParse(txt_SearchEmId.Text.Trim(), out int id))
            {
                emId = id;
            }

            string name = txt_SearchEmName.Text.Trim();
            string phone = txt_SearchPhone.Text.Trim();
            bool includeHidden = chk_IncludeHidden.IsChecked == true;

            int? soId = null;
            if (cmb_SearchOffice.SelectedValue is int sId && sId > 0)
            {
                soId = sId;
            }

            int? poId = null;
            if (cmb_SearchPosition.SelectedValue is int pId && pId > 0)
            {
                poId = pId;
            }

            var results = _employeeService.SearchEmployees(emId, name, soId, poId, phone, includeHidden);
            _cachedList = results;

            dg_Employees.ItemsSource = _cachedList;
            txt_RecordCount.Text = $"表示件数: {_cachedList.Count} 件";
            UpdateActionButtonsState();
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_SearchEmId.Text = string.Empty;
            txt_SearchEmName.Text = string.Empty;
            txt_SearchPhone.Text = string.Empty;
            cmb_SearchOffice.SelectedIndex = 0;
            cmb_SearchPosition.SelectedIndex = 0;
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

        private void UpdateActionButtonsState()
        {
            bool hasSelection = dg_Employees.SelectedItem is EmployeeDisplayModel;
            btn_EditSelected.IsEnabled = hasSelection;
            btn_HideSelected.IsEnabled = hasSelection;
        }

        private void dg_Employees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActionButtonsState();
        }

        private void dg_Employees_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg_Employees.SelectedItem is EmployeeDisplayModel selected)
            {
                OpenUpdateModal(selected.EmId);
            }
        }

        // =====================================================================
        // 新規登録
        // =====================================================================
        private void btn_NewRegister_Click(object sender, RoutedEventArgs e)
        {
            reg_txt_EmName.Text = string.Empty;
            reg_txt_Phone.Text = string.Empty;
            reg_txt_Password.Text = "1234";
            reg_dp_Hiredate.SelectedDate = DateTime.Today;

            if (_offices.Count > 0) reg_cmb_Office.SelectedIndex = 0;
            if (_positions.Count > 0) reg_cmb_Position.SelectedIndex = 0;

            modal_Register.Visibility = Visibility.Visible;
            reg_txt_EmName.Focus();
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
            string name = reg_txt_EmName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("社員名を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_EmName.Focus();
                return;
            }

            int soId = reg_cmb_Office.SelectedValue is int sId ? sId : 0;
            if (soId <= 0)
            {
                MessageBox.Show("所属営業所を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int poId = reg_cmb_Position.SelectedValue is int pId ? pId : 0;
            if (poId <= 0)
            {
                MessageBox.Show("役職を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string phone = reg_txt_Phone.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("電話番号を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_Phone.Focus();
                return;
            }

            string password = reg_txt_Password.Text.Trim();
            if (password.Length < 4 || password.Length > 10)
            {
                MessageBox.Show("パスワードは4文字以上10文字以内で入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                reg_txt_Password.Focus();
                return;
            }

            var newEmp = new MEmployee
            {
                EmName = name,
                SoId = soId,
                PoId = poId,
                EmHiredate = reg_dp_Hiredate.SelectedDate ?? DateTime.Today,
                EmPhone = phone,
                EmPassword = password
            };

            var (success, message, _) = _employeeService.RegisterEmployee(newEmp);
            if (success)
            {
                MessageBox.Show(message, "登録完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Register.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(message, "登録失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =====================================================================
        // 更新
        // =====================================================================
        private void btn_EditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Employees.SelectedItem is EmployeeDisplayModel selected)
            {
                OpenUpdateModal(selected.EmId);
            }
        }

        private void OpenUpdateModal(int emId)
        {
            var emp = _cachedList.FirstOrDefault(e => e.EmId == emId);
            if (emp == null) return;

            upd_txt_EmId.Text = emp.EmId.ToString();
            upd_txt_EmName.Text = emp.EmName;
            upd_cmb_Office.SelectedValue = emp.SoId;
            upd_cmb_Position.SelectedValue = emp.PoId;
            upd_dp_Hiredate.SelectedDate = emp.EmHiredate;
            upd_txt_Phone.Text = emp.EmPhone;
            upd_txt_Password.Text = string.Empty;

            modal_Update.Visibility = Visibility.Visible;
            upd_txt_EmName.Focus();
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
            if (!int.TryParse(upd_txt_EmId.Text, out int emId)) return;

            string name = upd_txt_EmName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("社員名を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                upd_txt_EmName.Focus();
                return;
            }

            int soId = upd_cmb_Office.SelectedValue is int sId ? sId : 0;
            if (soId <= 0)
            {
                MessageBox.Show("所属営業所を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int poId = upd_cmb_Position.SelectedValue is int pId ? pId : 0;
            if (poId <= 0)
            {
                MessageBox.Show("役職を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string phone = upd_txt_Phone.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("電話番号を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                upd_txt_Phone.Focus();
                return;
            }

            string newPassword = upd_txt_Password.Text.Trim();
            bool updatePassword = !string.IsNullOrEmpty(newPassword);

            if (updatePassword && (newPassword.Length < 4 || newPassword.Length > 10))
            {
                MessageBox.Show("パスワードは4文字以上10文字以内で入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                upd_txt_Password.Focus();
                return;
            }

            var emp = new MEmployee
            {
                EmId = emId,
                EmName = name,
                SoId = soId,
                PoId = poId,
                EmHiredate = upd_dp_Hiredate.SelectedDate ?? DateTime.Today,
                EmPhone = phone,
                EmPassword = newPassword
            };

            var (success, message) = _employeeService.UpdateEmployee(emp, updatePassword);
            if (success)
            {
                MessageBox.Show(message, "更新完了", MessageBoxButton.OK, MessageBoxImage.Information);
                modal_Update.Visibility = Visibility.Collapsed;
                ExecuteSearch();
            }
            else
            {
                MessageBox.Show(message, "更新失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =====================================================================
        // 非表示 (論理削除)
        // =====================================================================
        private void btn_HideSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Employees.SelectedItem is EmployeeDisplayModel selected)
            {
                _selectedEmployeeIdForHide = selected.EmId;
                hide_txt_TargetInfo.Text = $"社員ID: {selected.EmId} ／ 社員名: {selected.EmName}（{selected.SoName}）";
                hide_txt_Reason.Text = "退職による非表示設定";

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
            if (_selectedEmployeeIdForHide <= 0) return;

            string reason = hide_txt_Reason.Text.Trim();
            var (success, message) = _employeeService.HideEmployee(_selectedEmployeeIdForHide, reason);

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
