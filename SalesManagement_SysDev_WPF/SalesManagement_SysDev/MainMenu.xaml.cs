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
        private SharedMemoItem? _pendingConfidentialMemo; // 機密情報認証待ちのメモ
        private string _currentFilter = "All";

        public MainMenu()
        {
            InitializeComponent();
            lbl_LogonDate.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            // XAML初期化完了後にイベントを登録（パース中のNullReferenceExceptionを防止）
            rdo_FilterAll.Checked += Filter_Checked;
            rdo_FilterGeneral.Checked += Filter_Checked;
            rdo_FilterAdmin.Checked += Filter_Checked;
            rdo_FilterConfidential.Checked += Filter_Checked;

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
            lbl_SharedMemoAdminBadge.Text = isAdmin ? " [管理者権限: 投稿可] " : " [一般社員: 閲覧のみ] ";
            lbl_SharedMemoAdminBadge.Foreground = isAdmin ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Gray;
            btn_NewSharedMemo.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // 一般社員の場合は「管理者限定」フィルターの利用を制御
            rdo_FilterAdmin.IsEnabled = isAdmin;
            if (!isAdmin && rdo_FilterAdmin.IsChecked == true)
            {
                rdo_FilterAll.IsChecked = true;
            }

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

        #region ② 社内連絡事項 (全体メモ) - 一般連絡・管理者限定・機密情報の制御

        private void LoadSharedMemos()
        {
            _sharedMemos = _memoService.GetSharedMemos();
            ApplySharedMemoFilter();
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (rdo_FilterAll?.IsChecked == true) _currentFilter = "All";
            else if (rdo_FilterGeneral?.IsChecked == true) _currentFilter = "General";
            else if (rdo_FilterAdmin?.IsChecked == true) _currentFilter = "AdminOnly";
            else if (rdo_FilterConfidential?.IsChecked == true) _currentFilter = "Confidential";

            ApplySharedMemoFilter();
        }

        private void ApplySharedMemoFilter()
        {
            if (lst_SharedMemos == null) return;

            // 共有メモの一覧が null の場合は空の一覧として扱う
            var memos = _sharedMemos ?? new List<SharedMemoItem>();

            bool isAdmin = IsAdmin();

            // 1. 権限による基本フィルタリング
            var accessibleMemos = memos.Where(m =>
            {
                if (m == null)
                {
                    return false;
                }

                if (m.AccessLevel == "AdminOnly" && !isAdmin)
                {
                    return false;
                }

                return true;
            });

            // 2. 選択されたフィルターによる絞り込み
            var filtered = accessibleMemos.Where(m =>
            {
                return _currentFilter switch
                {
                    "General" => m.AccessLevel == "General",
                    "AdminOnly" => m.AccessLevel == "AdminOnly",
                    "Confidential" => m.AccessLevel == "Confidential",
                    _ => true
                };
            }).ToList();

            // 3. 一覧を更新
            lst_SharedMemos.ItemsSource = null;
            lst_SharedMemos.ItemsSource = filtered;

            // 4. 件数を更新
            if (lbl_SharedMemoCount != null)
            {
                lbl_SharedMemoCount.Text = $"{filtered.Count} 件";
            }

            // 5. 先頭の項目を選択
            if (filtered.Count > 0)
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
                AccessLevel = "General",
                Passcode = "9999",
                CreatedAt = DateTime.Now
            };

            modal_SharedMemoTitleText.Text = "新規 社内連絡事項の登録";
            txt_SharedMemoTitle.Text = "";
            txt_SharedMemoContent.Text = "";
            txt_SharedMemoTitle.IsReadOnly = false;
            txt_SharedMemoContent.IsReadOnly = false;

            cmb_SharedMemoPriority.IsEnabled = true;
            cmb_SharedMemoPriority.SelectedIndex = 0; // 重要

            cmb_SharedMemoAccessLevel.IsEnabled = true;
            cmb_SharedMemoAccessLevel.SelectedIndex = 0; // 一般連絡

            txt_SharedMemoPasscode.Text = "9999";
            lbl_SharedMemoPasscodeLabel.Visibility = Visibility.Collapsed;
            txt_SharedMemoPasscode.Visibility = Visibility.Collapsed;

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

            bool isAdmin = IsAdmin();

            // ========================================================
            // アクセス制御判定
            // ========================================================

            // 1. 一般的な業務連絡：そのまま閲覧可能
            if (selected.AccessLevel == "General")
            {
                ShowSharedMemoDetail(selected);
                return;
            }

            // 2. 管理者向けの重要な連絡：管理者のみ閲覧可能
            if (selected.AccessLevel == "AdminOnly")
            {
                if (!isAdmin)
                {
                    MessageBox.Show("この連絡事項は管理者限定です。\n一般社員アカウントでは閲覧できません。",
                                    "アクセス権限制限", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ShowSharedMemoDetail(selected);
                return;
            }

            // 3. 機密情報を含む連絡：追加のアクセス制限（パスコード認証）
            if (selected.AccessLevel == "Confidential")
            {
                if (!isAdmin)
                {
                    MessageBox.Show("この連絡事項は【機密情報】に指定されています。\n閲覧には管理者権限および認証コードが必要です。",
                                    "アクセス権限制限", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 機密情報のパスコード認証ダイアログを表示
                _pendingConfidentialMemo = selected;
                pwd_ConfidentialInput.Password = "";
                lbl_ConfidentialAuthError.Visibility = Visibility.Collapsed;
                modal_ConfidentialAuth.Visibility = Visibility.Visible;
                pwd_ConfidentialInput.Focus();
                return;
            }

            // デフォルト
            ShowSharedMemoDetail(selected);
        }

        private void ShowSharedMemoDetail(SharedMemoItem item)
        {
            _editingSharedMemo = item;
            bool isAdmin = IsAdmin();

            modal_SharedMemoTitleText.Text = isAdmin ? $"社内連絡事項の確認・編集 [{item.AccessLevelName}]"
                                                     : $"社内連絡事項の詳細（閲覧用） [{item.AccessLevelName}]";
            txt_SharedMemoTitle.Text = item.Title;
            txt_SharedMemoContent.Text = item.Content;

            // 優先度コンボボックスの設定
            foreach (ComboBoxItem cItem in cmb_SharedMemoPriority.Items)
            {
                if (cItem.Content?.ToString() == item.Priority)
                {
                    cmb_SharedMemoPriority.SelectedItem = cItem;
                    break;
                }
            }

            // アクセス区分コンボボックスの設定
            foreach (ComboBoxItem aItem in cmb_SharedMemoAccessLevel.Items)
            {
                if (aItem.Tag?.ToString() == item.AccessLevel)
                {
                    cmb_SharedMemoAccessLevel.SelectedItem = aItem;
                    break;
                }
            }

            txt_SharedMemoPasscode.Text = item.Passcode ?? "9999";
            bool isConfidential = item.AccessLevel == "Confidential";
            lbl_SharedMemoPasscodeLabel.Visibility = isConfidential && isAdmin ? Visibility.Visible : Visibility.Collapsed;
            txt_SharedMemoPasscode.Visibility = isConfidential && isAdmin ? Visibility.Visible : Visibility.Collapsed;

            lbl_SharedMemoMeta.Text = $"発信者: {item.AuthorName} ｜ 投稿日時: {item.DisplayDate} ｜ 区分: {item.AccessLevelName}";

            // 権限による編集可否制御
            txt_SharedMemoTitle.IsReadOnly = !isAdmin;
            txt_SharedMemoContent.IsReadOnly = !isAdmin;
            cmb_SharedMemoPriority.IsEnabled = isAdmin;
            cmb_SharedMemoAccessLevel.IsEnabled = isAdmin;
            txt_SharedMemoPasscode.IsReadOnly = !isAdmin;

            btn_SaveSharedMemo.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btn_DeleteSharedMemo.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            lbl_SharedMemoReadOnlyNotice.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;

            modal_SharedMemoDetail.Visibility = Visibility.Visible;
        }

        private void cmb_SharedMemoAccessLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_SharedMemoAccessLevel?.SelectedItem is ComboBoxItem item)
            {
                bool isConfidential = item.Tag?.ToString() == "Confidential";
                if (lbl_SharedMemoPasscodeLabel != null && txt_SharedMemoPasscode != null)
                {
                    lbl_SharedMemoPasscodeLabel.Visibility = isConfidential ? Visibility.Visible : Visibility.Collapsed;
                    txt_SharedMemoPasscode.Visibility = isConfidential ? Visibility.Visible : Visibility.Collapsed;
                }
            }
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

            if (cmb_SharedMemoAccessLevel.SelectedItem is ComboBoxItem selectedAccess)
            {
                _editingSharedMemo.AccessLevel = selectedAccess.Tag?.ToString() ?? "General";
            }

            if (_editingSharedMemo.AccessLevel == "Confidential")
            {
                string passcode = txt_SharedMemoPasscode.Text.Trim();
                _editingSharedMemo.Passcode = string.IsNullOrEmpty(passcode) ? "9999" : passcode;
            }

            _editingSharedMemo.AuthorName = _loggedInEmployee?.EmName ?? "管理者";
            _editingSharedMemo.AuthorEmpId = _loggedInEmployee?.EmId ?? 1;

            _memoService.UpsertSharedMemo(_editingSharedMemo);

            modal_SharedMemoDetail.Visibility = Visibility.Collapsed;
            LoadSharedMemos();

            MessageBox.Show($"全体メモを保存しました。\n区分: {_editingSharedMemo.AccessLevelName}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
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

        #region 機密情報アクセス認証ダイアログの制御

        private void btn_SubmitConfidentialAuth_Click(object sender, RoutedEventArgs e)
        {
            VerifyConfidentialPasscode();
        }

        private void pwd_ConfidentialInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerifyConfidentialPasscode();
            }
        }

        private void VerifyConfidentialPasscode()
        {
            if (_pendingConfidentialMemo == null)
            {
                modal_ConfidentialAuth.Visibility = Visibility.Collapsed;
                return;
            }

            string input = pwd_ConfidentialInput.Password.Trim();
            string expected = string.IsNullOrEmpty(_pendingConfidentialMemo.Passcode) ? "9999" : _pendingConfidentialMemo.Passcode;

            if (input == expected)
            {
                // 認証成功！
                modal_ConfidentialAuth.Visibility = Visibility.Collapsed;
                ShowSharedMemoDetail(_pendingConfidentialMemo);
            }
            else
            {
                // 認証失敗
                lbl_ConfidentialAuthError.Visibility = Visibility.Visible;
                pwd_ConfidentialInput.SelectAll();
                pwd_ConfidentialInput.Focus();
            }
        }

        private void btn_CancelConfidentialAuth_Click(object sender, RoutedEventArgs e)
        {
            modal_ConfidentialAuth.Visibility = Visibility.Collapsed;
            _pendingConfidentialMemo = null;
        }

        private void modal_ConfidentialAuth_Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            modal_ConfidentialAuth.Visibility = Visibility.Collapsed;
            _pendingConfidentialMemo = null;
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
