using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SalesManagement_SysDev
{
    public partial class MainMenu : UserControl
    {
        public event Action<string>? NavigationRequested;

        public void RequestNavigation(string tag)
        {
            NavigationRequested?.Invoke(tag);
        }

        private readonly MemoService _memoService = new();
        private MEmployee? _loggedInEmployee;

        // 個人メモ関連
        private List<PersonalMemoItem> _personalMemos = new();
        private PersonalMemoItem? _currentPersonalMemo;
        private DispatcherTimer? _saveMessageTimer;

        // 全体メモ関連
        private List<SharedMemoItem> _sharedMemos = new();
        private SharedMemoItem? _editingSharedMemo;

        public MainMenu()
        {
            InitializeComponent();
            lbl_LogonDate.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            // 初期化時に全体メモ・個人メモ（管理者デフォルト）をロード
            LoadSharedMemos();
            LoadPersonalMemos();
        }

        /// <summary>
        /// ログイン社員情報を反映し、メモや権限を初期化
        /// </summary>
        public void SetEmployeeInfo(MEmployee? employee)
        {
            _loggedInEmployee = employee;

            if (_loggedInEmployee != null)
            {
                lbl_User.Text = $"{_loggedInEmployee.EmName}";
                lbl_Store.Text = _loggedInEmployee.So?.SoName ?? $"店舗 #{_loggedInEmployee.SoId}";
                lbl_Role.Text = _loggedInEmployee.Po?.PoName ?? "一般";
                lbl_PersonalMemoHeader.Text = $"個人メモ（{_loggedInEmployee.EmName} 様専用）";
            }
            else
            {
                lbl_User.Text = "管理者";
                lbl_Store.Text = "本社営業所";
                lbl_Role.Text = "管理者";
                lbl_PersonalMemoHeader.Text = "個人メモ（管理者専用）";
            }

            lbl_LogonDate.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            // 権限ごとのUI制御
            bool isAdmin = IsAdmin();
            lbl_SharedMemoAdminBadge.Text = isAdmin ? " [管理者権限: 投稿可] " : " [閲覧専用] ";
            lbl_SharedMemoAdminBadge.Foreground = isAdmin ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Gray;
            btn_NewSharedMemo.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // 各種メモ再読み込み
            LoadPersonalMemos();
            LoadSharedMemos();
        }

        private bool IsAdmin()
        {
            if (_loggedInEmployee == null) return true; // デモ・デフォルト管理者
            if (_loggedInEmployee.PoId == 1) return true;
            if (!string.IsNullOrEmpty(_loggedInEmployee.Po?.PoName) && _loggedInEmployee.Po.PoName.Contains("管理")) return true;
            return false;
        }

        #region ① システムの使い方 (操作ガイド・閲覧用)

        private void btn_OpenHelpGuide_Click(object sender, RoutedEventArgs e)
        {
            modal_HelpGuide.Visibility = Visibility.Visible;
        }

        private void btn_CloseHelpGuide_Click(object sender, RoutedEventArgs e)
        {
            modal_HelpGuide.Visibility = Visibility.Collapsed;
        }

        private void modal_HelpGuide_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_HelpGuide.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region ② 社内連絡事項 (全体メモ)

        private void LoadSharedMemos()
        {
            _sharedMemos = _memoService.GetSharedMemos();
            lst_SharedMemos.ItemsSource = null;
            lst_SharedMemos.ItemsSource = _sharedMemos;

            if (_sharedMemos.Count > 0)
            {
                lst_SharedMemos.SelectedIndex = 0;
            }
        }

        private void btn_NewSharedMemo_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin())
            {
                MessageBox.Show("連絡事項の新規投稿は管理者のみ可能です。", "権限エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _editingSharedMemo = new SharedMemoItem
            {
                Id = Guid.NewGuid().ToString(),
                AuthorName = _loggedInEmployee?.EmName ?? "管理者",
                AuthorEmpId = _loggedInEmployee?.EmId ?? 1,
                Priority = "重要",
                CreatedAt = DateTime.Now
            };

            modal_SharedMemoTitleText.Text = "新規 社内連絡事項の登録";
            txt_SharedMemoTitle.Text = "";
            txt_SharedMemoContent.Text = "";
            txt_SharedMemoTitle.IsReadOnly = false;
            txt_SharedMemoContent.IsReadOnly = false;
            cmb_SharedMemoPriority.IsEnabled = true;
            cmb_SharedMemoPriority.SelectedIndex = 0; // 重要

            lbl_SharedMemoMeta.Text = $"発信者: {_editingSharedMemo.AuthorName} ｜ {DateTime.Now:yyyy/MM/dd HH:mm}";
            lbl_SharedMemoReadOnlyNotice.Visibility = Visibility.Collapsed;

            btn_SaveSharedMemo.Visibility = Visibility.Visible;
            btn_DeleteSharedMemo.Visibility = Visibility.Collapsed; // 新規作成時は削除不可

            modal_SharedMemoDetail.Visibility = Visibility.Visible;
            txt_SharedMemoTitle.Focus();
        }

        private void btn_ViewSharedMemoDetail_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedSharedMemo();
        }

        private void lst_SharedMemos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedSharedMemo();
        }

        private void OpenSelectedSharedMemo()
        {
            var selected = lst_SharedMemos.SelectedItem as SharedMemoItem;
            if (selected == null)
            {
                MessageBox.Show("閲覧する連絡事項を選択してください。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _editingSharedMemo = selected;
            bool isAdmin = IsAdmin();

            modal_SharedMemoTitleText.Text = isAdmin ? "社内連絡事項の確認・編集" : "社内連絡事項の詳細（閲覧用）";
            txt_SharedMemoTitle.Text = selected.Title;
            txt_SharedMemoContent.Text = selected.Content;

            // 優先度コンボボックスの設定
            foreach (ComboBoxItem item in cmb_SharedMemoPriority.Items)
            {
                if (item.Content?.ToString() == selected.Priority)
                {
                    cmb_SharedMemoPriority.SelectedItem = item;
                    break;
                }
            }

            lbl_SharedMemoMeta.Text = $"発信者: {selected.AuthorName} ｜ 投稿日時: {selected.DisplayDate}";

            // 権限による編集可否制御
            txt_SharedMemoTitle.IsReadOnly = !isAdmin;
            txt_SharedMemoContent.IsReadOnly = !isAdmin;
            cmb_SharedMemoPriority.IsEnabled = isAdmin;

            btn_SaveSharedMemo.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btn_DeleteSharedMemo.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            lbl_SharedMemoReadOnlyNotice.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;

            modal_SharedMemoDetail.Visibility = Visibility.Visible;
        }

        private void btn_SaveSharedMemo_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin()) return;

            string title = txt_SharedMemoTitle.Text.Trim();
            string content = txt_SharedMemoContent.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("タイトルを入力してください。", "入力チェック", MessageBoxButton.OK, MessageBoxImage.Warning);
                txt_SharedMemoTitle.Focus();
                return;
            }

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("連絡内容を入力してください。", "入力チェック", MessageBoxButton.OK, MessageBoxImage.Warning);
                txt_SharedMemoContent.Focus();
                return;
            }

            if (_editingSharedMemo == null)
            {
                _editingSharedMemo = new SharedMemoItem();
            }

            _editingSharedMemo.Title = title;
            _editingSharedMemo.Content = content;

            if (cmb_SharedMemoPriority.SelectedItem is ComboBoxItem selectedPriority)
            {
                _editingSharedMemo.Priority = selectedPriority.Content?.ToString() ?? "通常";
            }

            _editingSharedMemo.AuthorName = _loggedInEmployee?.EmName ?? "管理者";
            _editingSharedMemo.AuthorEmpId = _loggedInEmployee?.EmId ?? 1;

            _memoService.UpsertSharedMemo(_editingSharedMemo);

            modal_SharedMemoDetail.Visibility = Visibility.Collapsed;
            LoadSharedMemos();

            MessageBox.Show("全体メモ（連絡事項）を保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btn_DeleteSharedMemo_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin() || _editingSharedMemo == null) return;

            var result = MessageBox.Show($"連絡事項「{_editingSharedMemo.Title}」を削除してもよろしいですか？",
                                         "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _memoService.DeleteSharedMemo(_editingSharedMemo.Id);
                modal_SharedMemoDetail.Visibility = Visibility.Collapsed;
                LoadSharedMemos();
            }
        }

        private void btn_CloseSharedMemoModal_Click(object sender, RoutedEventArgs e)
        {
            modal_SharedMemoDetail.Visibility = Visibility.Collapsed;
        }

        private void modal_SharedMemoDetail_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_SharedMemoDetail.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region ③ 個人メモ (社員専用)

        private void LoadPersonalMemos()
        {
            int empId = _loggedInEmployee?.EmId ?? 0;
            _personalMemos = _memoService.GetPersonalMemos(empId);

            cmb_PersonalMemos.SelectionChanged -= cmb_PersonalMemos_SelectionChanged;
            cmb_PersonalMemos.ItemsSource = null;
            cmb_PersonalMemos.ItemsSource = _personalMemos;
            cmb_PersonalMemos.SelectionChanged += cmb_PersonalMemos_SelectionChanged;

            if (_personalMemos.Count > 0)
            {
                cmb_PersonalMemos.SelectedIndex = 0;
                DisplayPersonalMemo(_personalMemos[0]);
            }
            else
            {
                CreateNewPersonalMemo();
            }
        }

        private void DisplayPersonalMemo(PersonalMemoItem item)
        {
            _currentPersonalMemo = item;
            txt_PersonalMemoTitle.Text = item.Title;
            txt_PersonalMemoContent.Text = item.Content;
            lbl_PersonalMemoStatus.Text = $"最終更新: {item.DisplayDate}";
        }

        private void cmb_PersonalMemos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_PersonalMemos.SelectedItem is PersonalMemoItem item)
            {
                DisplayPersonalMemo(item);
            }
        }

        private void btn_NewPersonalMemo_Click(object sender, RoutedEventArgs e)
        {
            CreateNewPersonalMemo();
        }

        private void CreateNewPersonalMemo()
        {
            int empId = _loggedInEmployee?.EmId ?? 0;
            _currentPersonalMemo = new PersonalMemoItem
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = empId,
                Title = $"メモ ({DateTime.Now:MM/dd HH:mm})",
                Content = "",
                UpdatedAt = DateTime.Now
            };

            txt_PersonalMemoTitle.Text = _currentPersonalMemo.Title;
            txt_PersonalMemoContent.Text = "";
            lbl_PersonalMemoStatus.Text = "新規メモ（未保存）";
            txt_PersonalMemoContent.Focus();
        }

        private void btn_SavePersonalMemo_Click(object sender, RoutedEventArgs e)
        {
            int empId = _loggedInEmployee?.EmId ?? 0;

            if (_currentPersonalMemo == null)
            {
                _currentPersonalMemo = new PersonalMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    EmployeeId = empId
                };
            }

            string title = txt_PersonalMemoTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                title = "（無題のメモ）";
                txt_PersonalMemoTitle.Text = title;
            }

            _currentPersonalMemo.Title = title;
            _currentPersonalMemo.Content = txt_PersonalMemoContent.Text;
            _currentPersonalMemo.EmployeeId = empId;
            _currentPersonalMemo.UpdatedAt = DateTime.Now;

            _memoService.SavePersonalMemo(_currentPersonalMemo);

            lbl_PersonalMemoStatus.Text = $"最終更新: {_currentPersonalMemo.DisplayDate}";

            // 保存完了のアニメーション・表示
            lbl_PersonalMemoSavedMessage.Visibility = Visibility.Visible;
            _saveMessageTimer?.Stop();
            _saveMessageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _saveMessageTimer.Tick += (s, args) =>
            {
                lbl_PersonalMemoSavedMessage.Visibility = Visibility.Collapsed;
                _saveMessageTimer.Stop();
            };
            _saveMessageTimer.Start();

            // コンボボックス一覧の更新
            string currentId = _currentPersonalMemo.Id;
            _personalMemos = _memoService.GetPersonalMemos(empId);

            cmb_PersonalMemos.SelectionChanged -= cmb_PersonalMemos_SelectionChanged;
            cmb_PersonalMemos.ItemsSource = null;
            cmb_PersonalMemos.ItemsSource = _personalMemos;
            var matched = _personalMemos.FirstOrDefault(m => m.Id == currentId);
            if (matched != null)
            {
                cmb_PersonalMemos.SelectedItem = matched;
            }
            cmb_PersonalMemos.SelectionChanged += cmb_PersonalMemos_SelectionChanged;
        }

        private void btn_DeletePersonalMemo_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPersonalMemo == null) return;

            var result = MessageBox.Show($"選択中のメモ「{_currentPersonalMemo.DisplayTitle}」を削除しますか？",
                                         "メモの削除確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _memoService.DeletePersonalMemo(_currentPersonalMemo.Id);
                LoadPersonalMemos();
            }
        }

        #endregion


    }
}
