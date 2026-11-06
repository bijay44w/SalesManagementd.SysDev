using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public enum EmployeeOperationMode
    {
        Register,
        Update,
        List,
        Search,
        Hide
    }

    /// <summary>
    /// 社員管理の各操作画面。画面とDB操作は別クラスに分けている。
    /// </summary>
    public partial class EmployeeOperationWindow : Window
    {
        private readonly EmployeeOperationMode _mode;
        private readonly EmployeeDataAccess _employeeDataAccess = new();

        public EmployeeOperationWindow(EmployeeOperationMode mode)
        {
            InitializeComponent();
            _mode = mode;
            ConfigureMode();
            LoadChoices();
        }

        private void ConfigureMode()
        {
            switch (_mode)
            {
                case EmployeeOperationMode.Register:
                    Title = "社員登録";
                    TitleText.Text = "社員登録";
                    DescriptionText.Text = "社員情報を入力して登録します。";
                    SearchPanel.Visibility = Visibility.Collapsed;
                    EmployeeDataGrid.Visibility = Visibility.Collapsed;
                    ActionButton.Content = "登録";
                    break;

                case EmployeeOperationMode.Update:
                    Title = "社員更新";
                    TitleText.Text = "社員更新";
                    DescriptionText.Text = "一覧から社員を選択し、情報を更新します。";
                    EmployeeIdTextBox.IsReadOnly = true;
                    ActionButton.Content = "社員情報を更新";
                    LoadEmployeeData();
                    break;

                case EmployeeOperationMode.List:
                    Title = "社員一覧表示";
                    TitleText.Text = "社員一覧表示";
                    DescriptionText.Text = "登録されている社員情報を一覧で表示します。";
                    SearchPanel.Visibility = Visibility.Visible;
                    EditPanel.Visibility = Visibility.Collapsed;
                    LoadEmployeeData();
                    break;

                case EmployeeOperationMode.Search:
                    Title = "社員検索";
                    TitleText.Text = "社員検索";
                    DescriptionText.Text = "社員番号・氏名・営業所・役職・電話番号から検索します。";
                    SearchPanel.Visibility = Visibility.Visible;
                    EditPanel.Visibility = Visibility.Collapsed;
                    LoadEmployeeData();
                    SearchTextBox.Focus();
                    break;

                case EmployeeOperationMode.Hide:
                    Title = "社員非表示";
                    TitleText.Text = "社員非表示";
                    DescriptionText.Text = "一覧から社員を選択し、管理フラグで非表示にします。";
                    SearchPanel.Visibility = Visibility.Visible;
                    ActionButton.Content = "選択した社員を非表示";
                    ActionButton.Width = 180;
                    ActionButton.Background = System.Windows.Media.Brushes.IndianRed;
                    EmployeeIdTextBox.IsReadOnly = true;
                    EmployeeNameTextBox.IsReadOnly = true;
                    SalesOfficeComboBox.IsEnabled = false;
                    PositionComboBox.IsEnabled = false;
                    HireDatePicker.IsEnabled = false;
                    PhoneTextBox.IsReadOnly = true;
                    PasswordBox.IsEnabled = false;
                    LoadEmployeeData();
                    break;
            }
        }

        private void LoadChoices()
        {
            try
            {
                SalesOfficeComboBox.ItemsSource = _employeeDataAccess.GetSalesOfficeChoices();
                PositionComboBox.ItemsSource = _employeeDataAccess.GetPositionChoices();

                if (SalesOfficeComboBox.Items.Count > 0)
                    SalesOfficeComboBox.SelectedIndex = 0;
                if (PositionComboBox.Items.Count > 0)
                    PositionComboBox.SelectedIndex = 0;

                if (_mode == EmployeeOperationMode.Register)
                    HireDatePicker.SelectedDate = DateTime.Today;
            }
            catch (Exception ex)
            {
                ShowError("営業所・役職の一覧を取得できませんでした。データベース接続を確認してください。", ex);
            }
        }

        private void LoadEmployeeData(string? keyword = null)
        {
            try
            {
                EmployeeDataGrid.ItemsSource = _employeeDataAccess.GetEmployeeData(keyword);
            }
            catch (Exception ex)
            {
                ShowError("社員情報を取得できませんでした。データベース接続を確認してください。", ex);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployeeData(SearchTextBox.Text);
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            LoadEmployeeData();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LoadEmployeeData(SearchTextBox.Text);
        }

        private void EmployeeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmployeeDataGrid.SelectedItem is not EmployeeDisplayData selected)
                return;

            if (_mode == EmployeeOperationMode.Update || _mode == EmployeeOperationMode.Hide)
            {
                EmployeeIdTextBox.Text = selected.EmployeeId.ToString();
                EmployeeNameTextBox.Text = selected.EmployeeName;
                SalesOfficeComboBox.SelectedValue = selected.SalesOfficeId;
                PositionComboBox.SelectedValue = selected.PositionId;
                HireDatePicker.SelectedDate = selected.HireDate;
                PhoneTextBox.Text = selected.Phone;
                PasswordBox.Clear();

                // パスワードは一覧に表示しないため、更新時は入力を必須にする。
                if (_mode == EmployeeOperationMode.Update)
                    PasswordBox.ToolTip = "更新後のパスワードを入力してください（4～10文字）。";
            }
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (_mode)
                {
                    case EmployeeOperationMode.Register:
                        RegisterEmployee();
                        break;
                    case EmployeeOperationMode.Update:
                        UpdateEmployee();
                        break;
                    case EmployeeOperationMode.Hide:
                        HideEmployee();
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError("社員管理の処理中にエラーが発生しました。", ex);
            }
        }

        private bool TryBuildEmployee(out MEmployee employee)
        {
            employee = new MEmployee();

            if (!int.TryParse(EmployeeIdTextBox.Text, out int employeeId) || employeeId <= 0)
            {
                MessageBox.Show("社員番号は1以上の整数で入力してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmployeeIdTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmployeeNameTextBox.Text) ||
                EmployeeNameTextBox.Text.Trim().Length > 50)
            {
                MessageBox.Show("社員名を入力してください（50文字以内）。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmployeeNameTextBox.Focus();
                return false;
            }

            if (SalesOfficeComboBox.SelectedItem is not EmployeeOfficeChoice office)
            {
                MessageBox.Show("営業所を選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (PositionComboBox.SelectedItem is not EmployeePositionChoice position)
            {
                MessageBox.Show("役職を選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (HireDatePicker.SelectedDate is not DateTime hireDate)
            {
                MessageBox.Show("入社日を選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text) || PhoneTextBox.Text.Trim().Length > 13)
            {
                MessageBox.Show("電話番号を入力してください（13文字以内）。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return false;
            }

            if (PasswordBox.Password.Length < 4 || PasswordBox.Password.Length > 10)
            {
                MessageBox.Show("パスワードは4～10文字で入力してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            employee = new MEmployee
            {
                EmId = employeeId,
                EmName = EmployeeNameTextBox.Text.Trim(),
                SoId = office.SalesOfficeId,
                PoId = position.PositionId,
                EmHiredate = hireDate.Date,
                EmPassword = PasswordBox.Password,
                EmPhone = PhoneTextBox.Text.Trim(),
                EmFlag = EmployeeDataAccess.ActiveFlag
            };

            return true;
        }

        private void RegisterEmployee()
        {
            if (!TryBuildEmployee(out MEmployee employee))
                return;

            if (_employeeDataAccess.AddEmployeeData(employee, out string message))
            {
                MessageBox.Show(message, "社員登録",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            else
            {
                MessageBox.Show(message, "社員登録",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateEmployee()
        {
            if (EmployeeDataGrid.SelectedItem is not EmployeeDisplayData selected)
            {
                MessageBox.Show("更新する社員を一覧から選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryBuildEmployee(out MEmployee employee))
                return;

            if (employee.EmId != selected.EmployeeId)
            {
                MessageBox.Show("社員番号が一致しません。一覧から対象社員を選択し直してください。",
                    "入力確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_employeeDataAccess.UpdateEmployeeData(employee, out string message))
            {
                MessageBox.Show(message, "社員更新",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadEmployeeData();
            }
            else
            {
                MessageBox.Show(message, "社員更新",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void HideEmployee()
        {
            if (EmployeeDataGrid.SelectedItem is not EmployeeDisplayData selected)
            {
                MessageBox.Show("非表示にする社員を一覧から選択してください。", "入力確認",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var answer = MessageBox.Show(
                $"社員「{selected.EmployeeName}（社員番号：{selected.EmployeeId}）」を非表示にしますか？\n社員データ自体は削除されません。",
                "社員非表示の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            if (_employeeDataAccess.HideEmployeeData(selected.EmployeeId, out string message))
            {
                MessageBox.Show(message, "社員非表示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadEmployeeData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(message, "社員非表示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearForm()
        {
            EmployeeIdTextBox.Clear();
            EmployeeNameTextBox.Clear();
            PhoneTextBox.Clear();
            PasswordBox.Clear();
            HireDatePicker.SelectedDate = _mode == EmployeeOperationMode.Register ? DateTime.Today : null;
            EmployeeDataGrid.SelectedItem = null;
            if (SalesOfficeComboBox.Items.Count > 0)
                SalesOfficeComboBox.SelectedIndex = 0;
            if (PositionComboBox.Items.Count > 0)
                PositionComboBox.SelectedIndex = 0;
        }

        private static void ShowError(string message, Exception ex)
        {
            MessageBox.Show(
                $"{message}\n詳細: {ex.Message}",
                "社員管理エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
