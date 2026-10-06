using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    public partial class F_login : Window
    {
        private bool _isPasswordRevealed = false;
        private bool _isNewPasswordRevealed = false;
        private bool _isConfirmPasswordRevealed = false;

        private int _failedAttempts = 0;

        private readonly Brush _normalBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
        private readonly Brush _focusBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1877F2"));

        public F_login()
        {
            InitializeComponent();
        }

        #region Login Input Event Handlers & Watermarks

        private void txt_EmployeeId_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (watermark_Id != null)
            {
                watermark_Id.Visibility = string.IsNullOrEmpty(txt_EmployeeId.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearError();
        }

        private void txt_EmployeeId_GotFocus(object sender, RoutedEventArgs e)
        {
            border_Id.BorderBrush = _focusBorderBrush;
        }

        private void txt_EmployeeId_LostFocus(object sender, RoutedEventArgs e)
        {
            border_Id.BorderBrush = _normalBorderBrush;
        }

        private void txt_Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordRevealed && watermark_Password != null)
            {
                watermark_Password.Visibility = string.IsNullOrEmpty(txt_Password.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearError();
        }

        private void txt_Password_GotFocus(object sender, RoutedEventArgs e)
        {
            border_Password.BorderBrush = _focusBorderBrush;
        }

        private void txt_Password_LostFocus(object sender, RoutedEventArgs e)
        {
            border_Password.BorderBrush = _normalBorderBrush;
        }

        private void txt_PasswordRevealed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordRevealed && watermark_Password != null)
            {
                watermark_Password.Visibility = string.IsNullOrEmpty(txt_PasswordRevealed.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearError();
        }

        private void txt_PasswordRevealed_GotFocus(object sender, RoutedEventArgs e)
        {
            border_Password.BorderBrush = _focusBorderBrush;
        }

        private void txt_PasswordRevealed_LostFocus(object sender, RoutedEventArgs e)
        {
            border_Password.BorderBrush = _normalBorderBrush;
        }

        private void btn_TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordRevealed = !_isPasswordRevealed;

            if (_isPasswordRevealed)
            {
                txt_PasswordRevealed.Text = txt_Password.Password;
                txt_Password.Visibility = Visibility.Collapsed;
                txt_PasswordRevealed.Visibility = Visibility.Visible;
                txt_PasswordRevealed.Focus();
                txt_PasswordRevealed.CaretIndex = txt_PasswordRevealed.Text.Length;
                watermark_Password.Visibility = string.IsNullOrEmpty(txt_PasswordRevealed.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                txt_Password.Password = txt_PasswordRevealed.Text;
                txt_PasswordRevealed.Visibility = Visibility.Collapsed;
                txt_Password.Visibility = Visibility.Visible;
                txt_Password.Focus();
                watermark_Password.Visibility = string.IsNullOrEmpty(txt_Password.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        #endregion

        #region Login Processing & Authentication

        private void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            ClearError();

            string idInput = txt_EmployeeId.Text.Trim();
            string passwordInput = _isPasswordRevealed ? txt_PasswordRevealed.Text : txt_Password.Password;

            // 1. Validation
            if (string.IsNullOrEmpty(idInput))
            {
                ShowError("社員IDを入力してください。");
                txt_EmployeeId.Focus();
                return;
            }

            if (!int.TryParse(idInput, out int employeeId))
            {
                ShowError("社員IDは半角数字で入力してください。");
                txt_EmployeeId.Focus();
                return;
            }

            if (string.IsNullOrEmpty(passwordInput))
            {
                ShowError("パスワードを入力してください。");
                if (_isPasswordRevealed) txt_PasswordRevealed.Focus(); else txt_Password.Focus();
                return;
            }

            // 2. Database Authentication
            try
            {
                using var context = new SalesManagementContext();

                // Check employee by ID
                var employee = context.MEmployees
                    .Include(x => x.Po)
                    .Include(x => x.So)
                    .FirstOrDefault(x => x.EmId == employeeId);

                if (employee == null)
                {
                    _failedAttempts++;
                    CheckFailedAttempts(idInput, "指定された社員IDは登録されていません。");
                    return;
                }

                if (employee.EmFlag != 0)
                {
                    ShowError("この社員アカウントは無効化されています。管理者にご確認ください。");
                    return;
                }

                if (employee.EmPassword != passwordInput)
                {
                    _failedAttempts++;
                    CheckFailedAttempts(idInput, "パスワードが正しくありません。");
                    return;
                }

                // Login Successful -> Reset counter & Open Main/Home window
                _failedAttempts = 0;
                var homeWindow = new F_Home(employee);
                homeWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"データベース接続エラー: ({ex.Message})");
            }
        }

        private void CheckFailedAttempts(string idInput, string defaultMessage)
        {
            if (_failedAttempts >= 2)
            {
                // Trigger password reset after 2 failed attempts
                ShowResetPasswordView(idInput, "パスワードを2回間違えたため、パスワードの再設定が必要です。\n社員IDと新しいパスワードを入力してください。");
            }
            else
            {
                ShowError($"{defaultMessage}\n（注意: パスワードを2回間違えると再設定画面に移行します。残り1回）");
            }
        }

        private void btn_ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
        {
            ShowResetPasswordView(txt_EmployeeId.Text.Trim(), "社員IDと新しいパスワードを入力して再設定してください。");
        }

        private void ShowError(string message)
        {
            txt_ErrorMessage.Text = message;
            border_ErrorAlert.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            if (border_ErrorAlert != null)
            {
                border_ErrorAlert.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Forgot / Reset Password Logic

        private void ShowResetPasswordView(string prefilledId, string promptMessage)
        {
            ClearError();
            ClearResetError();

            txt_ResetSubtitle.Text = promptMessage;
            txt_ResetEmployeeId.Text = prefilledId;
            watermark_ResetId.Visibility = string.IsNullOrEmpty(prefilledId) ? Visibility.Visible : Visibility.Collapsed;

            // Clear password fields
            txt_NewPassword.Password = "";
            txt_NewPasswordRevealed.Text = "";
            watermark_NewPassword.Visibility = Visibility.Visible;

            txt_ConfirmPassword.Password = "";
            txt_ConfirmPasswordRevealed.Text = "";
            watermark_ConfirmPassword.Visibility = Visibility.Visible;

            panel_Login.Visibility = Visibility.Collapsed;
            panel_ResetPassword.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(prefilledId))
            {
                txt_ResetEmployeeId.Focus();
            }
            else
            {
                txt_NewPassword.Focus();
            }
        }

        private void btn_BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            ClearResetError();
            panel_ResetPassword.Visibility = Visibility.Collapsed;
            panel_Login.Visibility = Visibility.Visible;
            txt_EmployeeId.Focus();
        }

        // Reset ID Field Events
        private void txt_ResetEmployeeId_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (watermark_ResetId != null)
            {
                watermark_ResetId.Visibility = string.IsNullOrEmpty(txt_ResetEmployeeId.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearResetError();
        }

        private void txt_ResetEmployeeId_GotFocus(object sender, RoutedEventArgs e)
        {
            border_ResetId.BorderBrush = _focusBorderBrush;
        }

        private void txt_ResetEmployeeId_LostFocus(object sender, RoutedEventArgs e)
        {
            border_ResetId.BorderBrush = _normalBorderBrush;
        }

        // New Password Field Events
        private void txt_NewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isNewPasswordRevealed && watermark_NewPassword != null)
            {
                watermark_NewPassword.Visibility = string.IsNullOrEmpty(txt_NewPassword.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearResetError();
        }

        private void txt_NewPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            border_NewPassword.BorderBrush = _focusBorderBrush;
        }

        private void txt_NewPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            border_NewPassword.BorderBrush = _normalBorderBrush;
        }

        private void txt_NewPasswordRevealed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isNewPasswordRevealed && watermark_NewPassword != null)
            {
                watermark_NewPassword.Visibility = string.IsNullOrEmpty(txt_NewPasswordRevealed.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearResetError();
        }

        private void txt_NewPasswordRevealed_GotFocus(object sender, RoutedEventArgs e)
        {
            border_NewPassword.BorderBrush = _focusBorderBrush;
        }

        private void txt_NewPasswordRevealed_LostFocus(object sender, RoutedEventArgs e)
        {
            border_NewPassword.BorderBrush = _normalBorderBrush;
        }

        private void btn_ToggleNewPasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isNewPasswordRevealed = !_isNewPasswordRevealed;

            if (_isNewPasswordRevealed)
            {
                txt_NewPasswordRevealed.Text = txt_NewPassword.Password;
                txt_NewPassword.Visibility = Visibility.Collapsed;
                txt_NewPasswordRevealed.Visibility = Visibility.Visible;
                txt_NewPasswordRevealed.Focus();
                txt_NewPasswordRevealed.CaretIndex = txt_NewPasswordRevealed.Text.Length;
                watermark_NewPassword.Visibility = string.IsNullOrEmpty(txt_NewPasswordRevealed.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                txt_NewPassword.Password = txt_NewPasswordRevealed.Text;
                txt_NewPasswordRevealed.Visibility = Visibility.Collapsed;
                txt_NewPassword.Visibility = Visibility.Visible;
                txt_NewPassword.Focus();
                watermark_NewPassword.Visibility = string.IsNullOrEmpty(txt_NewPassword.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        // Confirm Password Field Events
        private void txt_ConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isConfirmPasswordRevealed && watermark_ConfirmPassword != null)
            {
                watermark_ConfirmPassword.Visibility = string.IsNullOrEmpty(txt_ConfirmPassword.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearResetError();
        }

        private void txt_ConfirmPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            border_ConfirmPassword.BorderBrush = _focusBorderBrush;
        }

        private void txt_ConfirmPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            border_ConfirmPassword.BorderBrush = _normalBorderBrush;
        }

        private void txt_ConfirmPasswordRevealed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isConfirmPasswordRevealed && watermark_ConfirmPassword != null)
            {
                watermark_ConfirmPassword.Visibility = string.IsNullOrEmpty(txt_ConfirmPasswordRevealed.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ClearResetError();
        }

        private void txt_ConfirmPasswordRevealed_GotFocus(object sender, RoutedEventArgs e)
        {
            border_ConfirmPassword.BorderBrush = _focusBorderBrush;
        }

        private void txt_ConfirmPasswordRevealed_LostFocus(object sender, RoutedEventArgs e)
        {
            border_ConfirmPassword.BorderBrush = _normalBorderBrush;
        }

        private void btn_ToggleConfirmPasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordRevealed = !_isConfirmPasswordRevealed;

            if (_isConfirmPasswordRevealed)
            {
                txt_ConfirmPasswordRevealed.Text = txt_ConfirmPassword.Password;
                txt_ConfirmPassword.Visibility = Visibility.Collapsed;
                txt_ConfirmPasswordRevealed.Visibility = Visibility.Visible;
                txt_ConfirmPasswordRevealed.Focus();
                txt_ConfirmPasswordRevealed.CaretIndex = txt_ConfirmPasswordRevealed.Text.Length;
                watermark_ConfirmPassword.Visibility = string.IsNullOrEmpty(txt_ConfirmPasswordRevealed.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                txt_ConfirmPassword.Password = txt_ConfirmPasswordRevealed.Text;
                txt_ConfirmPasswordRevealed.Visibility = Visibility.Collapsed;
                txt_ConfirmPassword.Visibility = Visibility.Visible;
                txt_ConfirmPassword.Focus();
                watermark_ConfirmPassword.Visibility = string.IsNullOrEmpty(txt_ConfirmPassword.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        // Submit Reset Password
        private void btn_ResetPasswordSubmit_Click(object sender, RoutedEventArgs e)
        {
            ClearResetError();

            string idInput = txt_ResetEmployeeId.Text.Trim();
            string newPassword = _isNewPasswordRevealed ? txt_NewPasswordRevealed.Text : txt_NewPassword.Password;
            string confirmPassword = _isConfirmPasswordRevealed ? txt_ConfirmPasswordRevealed.Text : txt_ConfirmPassword.Password;

            // 1. Validation
            if (string.IsNullOrEmpty(idInput))
            {
                ShowResetError("社員IDを入力してください。");
                txt_ResetEmployeeId.Focus();
                return;
            }

            if (!int.TryParse(idInput, out int employeeId))
            {
                ShowResetError("社員IDは半角数字で入力してください。");
                txt_ResetEmployeeId.Focus();
                return;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ShowResetError("新しいパスワードを入力してください。");
                if (_isNewPasswordRevealed) txt_NewPasswordRevealed.Focus(); else txt_NewPassword.Focus();
                return;
            }

            if (string.IsNullOrEmpty(confirmPassword))
            {
                ShowResetError("新しいパスワード（確認用）を入力してください。");
                if (_isConfirmPasswordRevealed) txt_ConfirmPasswordRevealed.Focus(); else txt_ConfirmPassword.Focus();
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowResetError("新しいパスワードと確認用パスワードが一致しません。");
                if (_isConfirmPasswordRevealed) txt_ConfirmPasswordRevealed.Focus(); else txt_ConfirmPassword.Focus();
                return;
            }

            // 2. Database Update
            try
            {
                using var context = new SalesManagementContext();

                var employee = context.MEmployees
                    .Include(x => x.Po)
                    .Include(x => x.So)
                    .FirstOrDefault(x => x.EmId == employeeId);

                if (employee == null)
                {
                    ShowResetError("指定された社員IDは登録されていません。");
                    return;
                }

                if (employee.EmFlag != 0)
                {
                    ShowResetError("この社員アカウントは無効化されています。管理者にご確認ください。");
                    return;
                }

                // Update password in database
                employee.EmPassword = newPassword;
                context.SaveChanges();

                // Reset failed attempts counter
                _failedAttempts = 0;

                MessageBox.Show($"社員ID: {employee.EmId}（{employee.EmName} 様）のパスワードを正常に変更しました。\nログインします。",
                    "パスワード変更完了", MessageBoxButton.OK, MessageBoxImage.Information);

                // Open Home screen
                var homeWindow = new F_Home(employee);
                homeWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowResetError($"パスワード更新中にエラーが発生しました: ({ex.Message})");
            }
        }

        private void ShowResetError(string message)
        {
            txt_ResetAlertMessage.Text = message;
            border_ResetAlert.Visibility = Visibility.Visible;
        }

        private void ClearResetError()
        {
            if (border_ResetAlert != null)
            {
                border_ResetAlert.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
        private void btn_CleateDabase_Click(object sender, RoutedEventArgs e)
        {
            //データベースの生成を行います．
            //再度実行する場合には，必ずデータベースの削除をしてから実行してください．

            using SalesManagementContext context = new SalesManagementContext();

            context.Database.EnsureCreated();

            List<MPosition> po = new List<MPosition>();
            {
                po.Add(new MPosition()
                {
                    PoName = "管理者",
                    PoFlag = 0,
                });
                po.Add(new MPosition()
                {
                    PoName = "営業",
                    PoFlag = 0,
                });
                po.Add(new MPosition()
                {
                    PoName = "物流",
                    PoFlag = 0,
                });
                context.MPositions.AddRange(po);
                context.SaveChanges();
            }

            MessageBox.Show("テーブル作成完了");
        }

        private void btn_InsertSampleData_Click(object sender, RoutedEventArgs e)
        {
            using SalesManagementContext context = new SalesManagementContext();

            List<MPosition> po = context.MPositions.OrderBy(x => x.PoId).ToList();
            List<MMaker> ma = new List<MMaker>();
            List<MSalesOffice> so = new List<MSalesOffice>();
            List<MClient> cl = new List<MClient>();
            Dictionary<int, MEmployee> em = new Dictionary<int, MEmployee>();
            List<MMajorClassification> mc = new List<MMajorClassification>();
            List<MSmallClassification> sc = new List<MSmallClassification>();
            List<MProduct> pr = new List<MProduct>();

            {
                ma.Add(new MMaker()
                {
                    MaName = "Aメーカ",
                    MaAddress = "大阪",
                    MaPhone = "000-0000-0000",
                    MaPostal = "0000000",
                    MaFax = "000-0000-0000",
                    MaFlag = 0,
                });
                ma.Add(new MMaker()
                {
                    MaName = "Bメーカ",
                    MaAddress = "京都",
                    MaPhone = "000-0000-0000",
                    MaPostal = "0000000",
                    MaFax = "000-0000-0000",
                    MaFlag = 0,
                });
                ma.Add(new MMaker()
                {
                    MaName = "Cメーカ",
                    MaAddress = "和歌山",
                    MaPhone = "000-0000-0000",
                    MaPostal = "0000000",
                    MaFax = "000-0000-0000",
                    MaFlag = 0,
                });
                ma.Add(new MMaker()
                {
                    MaName = "Dメーカ",
                    MaAddress = "滋賀",
                    MaPhone = "000-0000-0000",
                    MaPostal = "0000000",
                    MaFax = "000-0000-0000",
                    MaFlag = 0,
                });
                context.MMakers.AddRange(ma);
                context.SaveChanges();
            }
            {
                so.Add(new MSalesOffice()
                {
                    SoName = "北大阪営業所",
                    SoAddress = "大阪府吹田市寿町3-4-40",
                    SoPhone = "06-7011-6123",
                    SoPostal = "5600046",
                    SoFax = "06-6562-2740",
                    SoFlag = 0,
                });
                so.Add(new MSalesOffice()
                {
                    SoName = "兵庫営業所",
                    SoAddress = "兵庫県姫路市東辻井2-5-20",
                    SoPhone = "079-669-4326",
                    SoPostal = "6700994",
                    SoFax = "079-669-4327",
                    SoFlag = 0,
                });
                so.Add(new MSalesOffice()
                {
                    SoName = "鹿営業所",
                    SoAddress = "奈良県生駒郡三郷町勢野8-7-50",
                    SoPhone = "0745-99-0084",
                    SoPostal = "6360814",
                    SoFax = "0746-0-1160",
                    SoFlag = 0,
                });
                so.Add(new MSalesOffice()
                {
                    SoName = "京都営業所",
                    SoAddress = "京都府京都市山科区東野南井ノ上町10-3-7",
                    SoPhone = "077-672-6006",
                    SoPostal = "6078143",
                    SoFax = "0771-85-2574",
                    SoFlag = 0,
                });
                so.Add(new MSalesOffice()
                {
                    SoName = "和歌山営業所",
                    SoAddress = "和歌山県和歌山市柳丁4-19",
                    SoPhone = "073-887-1927",
                    SoPostal = "6408336",
                    SoFax = "0735-78-4874",
                    SoFlag = 0,
                });
                context.MSalesOffices.AddRange(so);
                context.SaveChanges();
            }
            {
                cl.Add(new MClient()
                {
                    ClName = "上村電機",
                    ClAddress = "京都府京都市伏見区塩屋町3-9-95",
                    ClPhone = "077-672-6006",
                    ClPostal = "6128046",
                    ClFax = "077-581-0164",
                    ClFlag = 0,
                    So = so[3],
                });
                cl.Add(new MClient()
                {
                    ClName = "萬田金融",
                    ClAddress = "大阪府大阪市西区北堀江1丁目22-3",
                    ClPhone = "06-8757-6267",
                    ClPostal = "5500014",
                    ClFax = "06-8757-6267",
                    ClFlag = 0,
                    So = so[0],
                });
                cl.Add(new MClient()
                {
                    ClName = "宝田電機",
                    ClAddress = "大阪府大阪市中央区和泉町2-5-46",
                    ClPhone = "06-1423-1895",
                    ClPostal = "5720806",
                    ClFax = "06-1374-4358",
                    ClFlag = 0,
                    So = so[0],
                });
                cl.Add(new MClient()
                {
                    ClName = "INATUGI",
                    ClAddress = "大阪府茨木市横江2-5-60",
                    ClPhone = "072-02-5171",
                    ClPostal = "5670044",
                    ClFax = "072-018-0116",
                    ClFlag = 0,
                    So = so[0],
                });
                cl.Add(new MClient()
                {
                    ClName = "水野電機",
                    ClAddress = "大阪府豊中市末広町2-6-13",
                    ClPhone = "06-2096-0974",
                    ClPostal = "5600024",
                    ClFax = "06-2434-2434",
                    ClFlag = 0,
                    So = so[1],
                });
                cl.Add(new MClient()
                {
                    ClName = "ショップ赤川",
                    ClAddress = "大阪府大阪市天王寺区上本町",
                    ClPhone = "090-1111-1111",
                    ClPostal = "5430001",
                    ClFax = "06-1111-1111",
                    ClFlag = 0,
                    So = so[0],
                });
                cl.Add(new MClient()
                {
                    ClName = "成田",
                    ClAddress = "奈良県御所市船路2-8-87",
                    ClPhone = "0746-0-1160",
                    ClPostal = "6392268",
                    ClFax = "0746-0-1160",
                    ClFlag = 0,
                    So = so[2],
                });
                context.MClients.AddRange(cl);
                context.SaveChanges();
            }
            {
                if (context.MEmployees.Where(x => x.EmId == 116).Count() == 0)
                {
                    em.Add(116, new MEmployee()
                    {
                        EmId = 116,
                        EmName = "坂口郁美",
                        EmHiredate = new DateTime(1980, 6, 17),
                        EmPassword = "0116",
                        EmPhone = "06-6813-5485",
                        So = so[1],
                        Po = po[2],
                    });
                }
                if (context.MEmployees.Where(x => x.EmId == 310).Count() == 0)
                {
                    em.Add(310, new MEmployee()
                    {
                        EmId = 310,
                        EmName = "高谷春男",
                        EmHiredate = new DateTime(1973, 3, 21),
                        EmPassword = "0310",
                        EmPhone = "06-6356-8742",
                        So = so[0],
                        Po = po[1],
                    });
                }
                if (context.MEmployees.Where(x => x.EmId == 1002).Count() == 0)
                {
                    em.Add(1002, new MEmployee()
                    {
                        EmId = 1002,
                        EmName = "日下部俊夫",
                        EmHiredate = new DateTime(1990, 9, 4),
                        EmPassword = "1002",
                        EmPhone = "06-6579-0622",
                        So = so[0],
                        Po = po[1],
                    });
                }
                if (context.MEmployees.Where(x => x.EmId == 1007).Count() == 0)
                {
                    em.Add(1007, new MEmployee()
                    {
                        EmId = 1007,
                        EmName = "岸本芽生",
                        EmHiredate = new DateTime(1997, 2, 4),
                        EmPassword = "1007",
                        EmPhone = "075-425-3371",
                        So = so[2],
                        Po = po[1],
                    });
                }
                if (context.MEmployees.Where(x => x.EmId == 1111).Count() == 0)
                {
                    em.Add(1111, new MEmployee()
                    {
                        EmId = 1111,
                        EmName = "奥村敦彦",
                        EmHiredate = new DateTime(1985, 3, 17),
                        EmPassword = "999",
                        EmPhone = "079-145-6121",
                        So = so[3],
                        Po = po[2],
                    });
                }
                if (context.MEmployees.Where(x => x.EmId == 1208).Count() == 0)
                {
                    em.Add(1208, new MEmployee()
                    {
                        EmId = 1208,
                        EmName = "渋谷秋昴",
                        EmHiredate = new DateTime(1994, 1, 31),
                        EmPassword = "1208",
                        EmPhone = "0790-68-8043",
                        So = so[4],
                        Po = po[1],
                    });
                }
                if (context.MEmployees.Where(x => x.EmId == 1227).Count() == 0)
                {
                    em.Add(1227, new MEmployee()
                    {
                        EmId = 1227,
                        EmName = "生田徳次郎",
                        EmHiredate = new DateTime(1964, 3, 20),
                        EmPassword = "1227",
                        EmPhone = "06-3021-1630",
                        So = so[0],
                        Po = po[0],
                    });
                }
                context.MEmployees.AddRange(em.Values);
                context.SaveChanges();
                foreach (var emp in context.MEmployees)
                {
                    em[emp.EmId] = emp;
                }
            }
            {
                mc.Add(new MMajorClassification()
                {
                    McName = "テレビ・レコーダー",
                    McFlag = 0,
                });
                mc.Add(new MMajorClassification()
                {
                    McName = "エアコン・冷蔵庫・洗濯機",
                    McFlag = 0,
                });
                mc.Add(new MMajorClassification()
                {
                    McName = "オーディオ・イヤホン・ヘッドホン",
                    McFlag = 0,
                });
                mc.Add(new MMajorClassification()
                {
                    McName = "携帯電話・スマートフォン",
                    McFlag = 0,
                });
                context.MMajorClassifications.AddRange(mc);
                context.SaveChanges();
            }
            {
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[0],
                    ScName = "テレビ",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[0],
                    ScName = "レコーダー",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[1],
                    ScName = "エアコン",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[1],
                    ScName = "冷蔵庫",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[1],
                    ScName = "洗濯機",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[2],
                    ScName = "オーディオ",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[2],
                    ScName = "イヤホン",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[2],
                    ScName = "ヘッドホン",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[3],
                    ScName = "携帯電話",
                    ScFlag = 0,
                });
                sc.Add(new MSmallClassification()
                {
                    Mc = mc[3],
                    ScName = "スマートフォン",
                    ScFlag = 0,
                });
                context.MSmallClassifications.AddRange(sc);
                context.SaveChanges();
            }
            {
                pr.Add(new MProduct()
                {
                    Ma = ma[0],
                    PrName = "テレビA",
                    Price = 100000,
                    PrSafetyStock = 100,
                    Sc = sc[0],
                    PrModelNumber = "1",
                    PrColor = "黒",
                    PrReleaseDate = new DateTime(2019, 5, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[0],
                    PrName = "テレビB",
                    Price = 98000,
                    PrSafetyStock = 100,
                    Sc = sc[0],
                    PrModelNumber = "1",
                    PrColor = "黒",
                    PrReleaseDate = new DateTime(2019, 5, 10),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[0],
                    PrName = "レコーダーA",
                    Price = 5000,
                    PrSafetyStock = 50,
                    Sc = sc[1],
                    PrModelNumber = "1",
                    PrColor = "黒",
                    PrReleaseDate = new DateTime(2019, 10, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[1],
                    PrName = "エアコンA",
                    Price = 160000,
                    PrSafetyStock = 50,
                    Sc = sc[2],
                    PrModelNumber = "1",
                    PrColor = "白",
                    PrReleaseDate = new DateTime(2020, 10, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[1],
                    PrName = "冷蔵庫A",
                    Price = 200000,
                    PrSafetyStock = 50,
                    Sc = sc[3],
                    PrModelNumber = "1",
                    PrColor = "白",
                    PrReleaseDate = new DateTime(2020, 1, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[1],
                    PrName = "洗濯機A",
                    Price = 150000,
                    PrSafetyStock = 50,
                    Sc = sc[4],
                    PrModelNumber = "1",
                    PrColor = "白",
                    PrReleaseDate = new DateTime(2019, 3, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[2],
                    PrName = "オーディオA",
                    Price = 6000,
                    PrSafetyStock = 10,
                    Sc = sc[5],
                    PrModelNumber = "1",
                    PrColor = "黒",
                    PrReleaseDate = new DateTime(2020, 8, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[2],
                    PrName = "イヤホンA",
                    Price = 5000,
                    PrSafetyStock = 100,
                    Sc = sc[6],
                    PrModelNumber = "1",
                    PrColor = "赤",
                    PrReleaseDate = new DateTime(2019, 5, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[3],
                    PrName = "iphone8",
                    Price = 78800,
                    PrSafetyStock = 50,
                    Sc = sc[8],
                    PrModelNumber = "1",
                    PrColor = "ゴールド",
                    PrReleaseDate = new DateTime(2017, 9, 22),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[3],
                    PrName = "スマートフォンA",
                    Price = 30000,
                    PrSafetyStock = 50,
                    Sc = sc[9],
                    PrModelNumber = "1",
                    PrColor = "シルバー",
                    PrReleaseDate = new DateTime(2019, 5, 1),
                    PrFlag = 0,
                });
                pr.Add(new MProduct()
                {
                    Ma = ma[3],
                    PrName = "スマートフォンB",
                    Price = 40000,
                    PrSafetyStock = 50,
                    Sc = sc[9],
                    PrModelNumber = "1",
                    PrColor = "黒",
                    PrReleaseDate = new DateTime(2020, 11, 1),
                    PrFlag = 0,
                });
                context.MProducts.AddRange(pr);
                context.SaveChanges();
            }
            List<TStock> st = new List<TStock>();
            {
                st.Add(new TStock()
                {
                    Pr = pr[0],
                    StQuantity = 100,
                    StFlag = 0,
                });
                st.Add(new TStock()
                {
                    Pr = pr[1],
                    StQuantity = 120,
                    StFlag = 0,
                });
                st.Add(new TStock()
                {
                    Pr = pr[2],
                    StQuantity = 199,
                    StFlag = 0,
                });
                st.Add(new TStock()
                {
                    Pr = pr[3],
                    StQuantity = 50,
                    StFlag = 0,
                });
                st.Add(new TStock()
                {
                    Pr = pr[4],
                    StQuantity = 60,
                    StFlag = 0,
                });
                st.Add(new TStock()
                {
                    Pr = pr[5],
                    StQuantity = 32,
                    StFlag = 0,
                });
                st.Add(new TStock()
                {
                    Pr = pr[9],
                    StQuantity = 240,
                    StFlag = 0,
                });
                context.TStocks.AddRange(st);
                context.SaveChanges();
            }
            List<TOrder> or = new List<TOrder>();
            {
                or.Add(new TOrder
                {
                    So = so[0],
                    Em = em[310],
                    Cl = cl[1],
                    ClCharge = "萬田銀次郎",
                    OrDate = new DateTime(2020, 12, 10),
                    OrStateFlag = 1,
                    OrFlag = 0,
                });
                or.Add(new TOrder
                {
                    So = so[1],
                    Em = em[116],
                    Cl = cl[4],
                    ClCharge = "水野勝成",
                    OrDate = new DateTime(2021, 1, 5),
                    OrStateFlag = 0,
                    OrFlag = 0,
                });
                context.TOrders.AddRange(or);
                context.SaveChanges();
            }
            List<TOrderDetail> ord = new List<TOrderDetail>();
            {
                ord.Add(new TOrderDetail()
                {
                    Or = or[0],
                    Pr = pr[2],
                    OrQuantity = 40,
                    OrTotalPrice = 200000,
                });
                ord.Add(new TOrderDetail()
                {
                    Or = or[0],
                    Pr = pr[9],
                    OrQuantity = 30,
                    OrTotalPrice = 900000,
                });
                ord.Add(new TOrderDetail()
                {
                    Or = or[1],
                    Pr = pr[3],
                    OrQuantity = 20,
                    OrTotalPrice = 3200000,
                });
                ord.Add(new TOrderDetail()
                {
                    Or = or[1],
                    Pr = pr[4],
                    OrQuantity = 15,
                    OrTotalPrice = 3000000,
                });
                ord.Add(new TOrderDetail()
                {
                    Or = or[1],
                    Pr = pr[5],
                    OrQuantity = 15,
                    OrTotalPrice = 2250000,
                });
                context.TOrderDetails.AddRange(ord);
                context.SaveChanges();
            }
            List<TChumon> ch = new List<TChumon>();
            {
                ch.Add(new TChumon()
                {
                    So = so[0],
                    Em = em[1002],
                    Cl = cl[1],
                    Or = or[0],
                    ChDate = new DateTime(2020, 12, 11),
                    ChStateFlag = 1,
                    ChFlag = 0,
                });
                context.TChumons.AddRange(ch);
                context.SaveChanges();
            }
            List<TChumonDetail> chd = new List<TChumonDetail>();
            {
                chd.Add(new TChumonDetail()
                {
                    Ch = ch[0],
                    Pr = pr[2],
                    ChQuantity = 40,
                });
                chd.Add(new TChumonDetail()
                {
                    Ch = ch[0],
                    Pr = pr[9],
                    ChQuantity = 30,
                });
                context.TChumonDetails.AddRange(chd);
                context.SaveChanges();
            }
            List<TSyukko> sy = new List<TSyukko>();
            {
                sy.Add(new TSyukko()
                {
                    Cl = cl[1],
                    So = so[0],
                    Or = or[0],
                    SyStateFlag = 0,
                    SyFlag = 0,
                });
                context.TSyukkos.AddRange(sy);
                context.SaveChanges();
            }
            List<TSyukkoDetail> syd = new List<TSyukkoDetail>();
            {
                syd.Add(new TSyukkoDetail()
                {
                    Sy = sy[0],
                    Pr = pr[2],
                    SyQuantity = 40,
                });
                syd.Add(new TSyukkoDetail()
                {
                    Sy = sy[0],
                    Pr = pr[9],
                    SyQuantity = 30,
                });
                context.TSyukkoDetails.AddRange(syd);
                context.SaveChanges();
            }

            MessageBox.Show("サンプルデータ登録完了");
        }
    }
}
