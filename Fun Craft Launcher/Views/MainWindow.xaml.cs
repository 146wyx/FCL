using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FunCraftLauncher.Models;
using FunCraftLauncher.Services;
using System.Linq;

namespace FunCraftLauncher.Views
{
    public partial class MainWindow : Window
    {
        public AuthResult? CurrentUser { get; private set; }
        private LoginType _selectedLoginType = LoginType.Microsoft;
        private readonly MinecraftAuthService _minecraftAuthService;
        private readonly MicrosoftAuthService _microsoftAuthService;

        public MainWindow()
        {
            InitializeComponent();
            _microsoftAuthService = new MicrosoftAuthService();
            _minecraftAuthService = new MinecraftAuthService();
            LogService.Instance.WriteInfo("MainWindow", "应用程序已启动");
            UpdateUI();
        }

        #region 窗口拖动

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        #endregion

        #region 登录类型切换

        private void PremiumLoginButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedLoginType = LoginType.Microsoft;
            UpdateLoginTypeButtons();
            ShowLoginPanel(LoginType.Microsoft);
        }

        private void OfflineLoginButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedLoginType = LoginType.Offline;
            UpdateLoginTypeButtons();
            ShowLoginPanel(LoginType.Offline);
        }

        private void UpdateLoginTypeButtons()
        {
            if (_selectedLoginType == LoginType.Microsoft)
            {
                PremiumCheck.Visibility = Visibility.Visible;
                OfflineCheck.Visibility = Visibility.Collapsed;
            }
            else
            {
                PremiumCheck.Visibility = Visibility.Collapsed;
                OfflineCheck.Visibility = Visibility.Visible;
            }
        }

        private void ShowLoginPanel(LoginType loginType)
        {
            if (CurrentUser != null) return;

            if (loginType == LoginType.Microsoft)
            {
                PremiumLoginPanel.Visibility = Visibility.Visible;
                OfflineLoginPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                PremiumLoginPanel.Visibility = Visibility.Collapsed;
                OfflineLoginPanel.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region 登录确认按钮事件

        private void PremiumConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DoMicrosoftLogin();
        }

        private void OfflineConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("MainWindow", "开始离线登录流程");
            string? username = OfflineUsernameTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                LogService.Instance.WriteWarning("MainWindow", "离线登录失败：用户名为空");
                MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (username.Length < 3 || username.Length > 16)
            {
                LogService.Instance.WriteWarning("MainWindow", $"离线登录失败：用户名长度不符合要求 (当前长度: {username.Length})");
                MessageBox.Show("用户名长度必须在 3-16 个字符之间", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string uuid = GenerateOfflineUUID(username);
            LogService.Instance.WriteInfo("MainWindow", $"为用户 {username} 生成离线UUID: {uuid}");

            CurrentUser = new AuthResult
            {
                LoginType = LoginType.Offline,
                Username = username,
                UUID = uuid,
                AccessToken = "0",
                IsSuccessful = true
            };

            LogService.Instance.WriteInfo("MainWindow", $"离线登录成功！用户名: {username}, UUID: {uuid}");
            UpdateUI();
        }

        #endregion

        #region 登录功能

        private async void DoMicrosoftLogin()
        {
            Window? dialog = null;
            try
            {
                LogService.Instance.WriteInfo("MainWindow", "开始Microsoft登录流程");
                // 显示加载状态
                PremiumConfirmButton.IsEnabled = false;
                PremiumConfirmButton.Content = "登录中...";

                // 1. 获取设备代码
                LogService.Instance.WriteInfo("MainWindow", "正在获取设备代码");
                var deviceCodeResult = await _microsoftAuthService.GetDeviceCodeAsync();
                LogService.Instance.WriteInfo("MainWindow", "设备代码获取成功");

                // 2. 显示设备代码对话框
                Dispatcher.Invoke(() =>
                {
                    dialog = ShowDeviceCodeDialog(deviceCodeResult.UserCode, deviceCodeResult.VerificationUrl);
                    
                    // 自动复制代码和打开浏览器
                    try
                    {
                        Clipboard.SetText(deviceCodeResult.UserCode);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(deviceCodeResult.VerificationUrl) { UseShellExecute = true });
                        LogService.Instance.WriteInfo("MainWindow", "已自动复制代码并打开浏览器");
                    }
                    catch (Exception ex)
                    {
                        LogService.Instance.WriteError("MainWindow", "自动复制代码或打开浏览器失败", ex);
                    }
                });

                // 3. 轮询获取 token
                LogService.Instance.WriteInfo("MainWindow", "正在轮询获取Microsoft token");
                var microsoftToken = await _microsoftAuthService.GetTokenAsync(
                    deviceCodeResult.DeviceCode,
                    deviceCodeResult.Interval,
                    deviceCodeResult.ExpiresIn
                );
                LogService.Instance.WriteInfo("MainWindow", "Microsoft token获取成功");

                // 4. 关闭对话框
                Dispatcher.Invoke(() => dialog?.Close());

                // 5. 使用获取的 token 进行 Minecraft 认证
                LogService.Instance.WriteInfo("MainWindow", "正在进行Minecraft认证");
                var minecraftAuth = await _minecraftAuthService.AuthenticateAsync(microsoftToken.AccessToken);

                if (minecraftAuth != null && minecraftAuth.IsSuccessful)
                {
                    CurrentUser = minecraftAuth;
                    LogService.Instance.WriteInfo("MainWindow", $"登录成功！用户名: {minecraftAuth.Username}, UUID: {minecraftAuth.UUID}");
                    Dispatcher.Invoke(() => UpdateUI());
                    MessageBox.Show($"登录成功！\n用户名: {minecraftAuth.Username}\nUUID: {minecraftAuth.UUID}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LogService.Instance.WriteError("MainWindow", "Minecraft认证返回空结果或失败");
                    MessageBox.Show("Minecraft 认证返回空结果", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("MainWindow", "Microsoft登录失败", ex);
                Dispatcher.Invoke(() => dialog?.Close());
                MessageBox.Show($"登录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    PremiumConfirmButton.IsEnabled = true;
                    PremiumConfirmButton.Content = "登录";
                });
            }
        }

        /// <summary>
        /// 显示设备代码对话框
        /// </summary>
        private Window? ShowDeviceCodeDialog(string userCode, string verificationUri)
        {
            var dialog = new Window
            {
                Title = "Microsoft 登录",
                Width = 450,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 44))
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = "请在浏览器中完成登录",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(titleBlock, 0);

            var descBlock = new TextBlock
            {
                Text = "1. 访问以下网址：",
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(descBlock, 1);

            var uriBlock = new TextBlock
            {
                Text = verificationUri,
                Foreground = System.Windows.Media.Brushes.LightBlue,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15),
                Cursor = Cursors.Hand,
                TextDecorations = TextDecorations.Underline
            };
            uriBlock.MouseLeftButtonDown += (s, e) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(verificationUri) { UseShellExecute = true });
            };
            Grid.SetRow(uriBlock, 2);

            var codeDescBlock = new TextBlock
            {
                Text = "2. 输入以下代码（已自动复制）：",
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(codeDescBlock, 3);

            var codeBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var codeBlock = new TextBlock
            {
                Text = userCode,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            codeBorder.Child = codeBlock;
            Grid.SetRow(codeBorder, 4);

            grid.Children.Add(titleBlock);
            grid.Children.Add(descBlock);
            grid.Children.Add(uriBlock);
            grid.Children.Add(codeDescBlock);
            grid.Children.Add(codeBorder);

            dialog.Content = grid;
            dialog.Show();

            return dialog;
        }

        private string GenerateOfflineUUID(string username)
        {
            string offlinePlayerString = "OfflinePlayer:" + username;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(offlinePlayerString);

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(bytes);
                hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        #endregion

        #region UI 更新

        private void UpdateUI()
        {
            if (CurrentUser == null)
            {
                LoggedInInfoPanel.Visibility = Visibility.Collapsed;
                RightContentPanel.Visibility = Visibility.Collapsed;
                PremiumPlayerAvatar.Visibility = Visibility.Collapsed;
                PremiumPlayerNameText.Visibility = Visibility.Collapsed;
                Title = "FCL";
            }
            else
            {
                if (CurrentUser.LoginType == LoginType.Microsoft)
                {
                    // 正版登录 - 在登录面板显示玩家信息
                    PremiumConfirmButton.Visibility = Visibility.Collapsed;
                    // 显示头像（使用用户名字母）
                    PremiumPlayerAvatarText.Text = CurrentUser.Username[0].ToString().ToUpper();
                    PremiumPlayerAvatar.Visibility = Visibility.Visible;
                    // 显示玩家ID
                    PremiumPlayerNameText.Text = CurrentUser.Username;
                    PremiumPlayerNameText.Visibility = Visibility.Visible;
                }
                else
                {
                    // 离线登录 - 隐藏登录面板
                    PremiumLoginPanel.Visibility = Visibility.Collapsed;
                    OfflineLoginPanel.Visibility = Visibility.Collapsed;
                }

                LoggedInInfoPanel.Visibility = Visibility.Visible;
                RightContentPanel.Visibility = Visibility.Visible;
                UsernameText.Text = CurrentUser.Username;
                LoginTypeText.Text = CurrentUser.LoginType == LoginType.Microsoft ? "正版登录" : "离线登录";
                AvatarText.Text = CurrentUser.Username[0].ToString().ToUpper();
                WelcomeText.Text = $"欢迎回来，{CurrentUser.Username}";
                Title = $"FCL - {CurrentUser.Username}";
            }

            UpdateLoginTypeButtons();
        }

        #endregion

        #region 按钮事件

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("MainWindow", "用户点击了退出登录按钮");
            var result = MessageBox.Show("确定要退出登录吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LogService.Instance.WriteInfo("MainWindow", "用户确认退出登录");
                CurrentUser = null;
                LoggedInInfoPanel.Visibility = Visibility.Collapsed;
                RightContentPanel.Visibility = Visibility.Collapsed;
                PremiumPlayerAvatar.Visibility = Visibility.Collapsed;
                PremiumPlayerNameText.Visibility = Visibility.Collapsed;
                PremiumConfirmButton.Visibility = Visibility.Visible;
                _selectedLoginType = LoginType.Microsoft;
                PremiumLoginPanel.Visibility = Visibility.Visible;
                OfflineLoginPanel.Visibility = Visibility.Collapsed;
                UpdateLoginTypeButtons();
                Title = "FCL";
                LogService.Instance.WriteInfo("MainWindow", "退出登录成功");
            }
            else
            {
                LogService.Instance.WriteInfo("MainWindow", "用户取消退出登录");
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("MainWindow", "用户点击了启动游戏按钮");
            if (CurrentUser == null)
            {
                LogService.Instance.WriteWarning("MainWindow", "启动游戏失败：用户未登录");
                MessageBox.Show("请先选择登录方式", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string loginType = CurrentUser.LoginType == LoginType.Microsoft ? "正版" : "离线";
            LogService.Instance.WriteInfo("MainWindow", $"启动游戏：用户名: {CurrentUser.Username}, 登录方式: {loginType}");
            MessageBox.Show($"启动游戏功能开发中...\n用户名: {CurrentUser.Username}\n登录方式: {loginType}",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("MainWindow", "用户点击了关闭按钮");
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            LogService.Instance.WriteInfo("MainWindow", "应用程序正在关闭");
            LogService.Instance.Dispose();
        }

        #endregion
    }
}
