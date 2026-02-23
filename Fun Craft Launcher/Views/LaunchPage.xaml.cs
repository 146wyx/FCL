using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FunCraftLauncher.Models;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Views
{
    public partial class LaunchPage : UserControl
    {
        private LoginType _selectedLoginType = LoginType.Microsoft;
        private AuthResult? _currentUser;

        public LaunchPage()
        {
            InitializeComponent();
            Loaded += LaunchPage_Loaded;
        }

        private void LaunchPage_Loaded(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("LaunchPage", "启动页面已加载");
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_currentUser == null)
            {
                LoggedInInfoPanel.Visibility = Visibility.Collapsed;
                if (RightContentPanel != null)
                    RightContentPanel.Visibility = Visibility.Collapsed;
                PremiumPlayerAvatar.Visibility = Visibility.Collapsed;
                PremiumPlayerNameText.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (_currentUser.LoginType == LoginType.Microsoft)
                {
                    PremiumConfirmButton.Visibility = Visibility.Collapsed;
                    PremiumPlayerAvatarText.Text = _currentUser.Username[0].ToString().ToUpper();
                    PremiumPlayerAvatar.Visibility = Visibility.Visible;
                    PremiumPlayerNameText.Text = _currentUser.Username;
                    PremiumPlayerNameText.Visibility = Visibility.Visible;
                }
                else
                {
                    PremiumLoginPanel.Visibility = Visibility.Collapsed;
                    OfflineLoginPanel.Visibility = Visibility.Collapsed;
                }

                LoggedInInfoPanel.Visibility = Visibility.Visible;
                if (RightContentPanel != null)
                    RightContentPanel.Visibility = Visibility.Visible;
                UsernameText.Text = _currentUser.Username;
                LoginTypeText.Text = _currentUser.LoginType == LoginType.Microsoft ? "正版登录" : "离线登录";
                AvatarText.Text = _currentUser.Username[0].ToString().ToUpper();
                if (WelcomeText != null)
                    WelcomeText.Text = $"欢迎回来，{_currentUser.Username}";
            }

            UpdateLoginTypeButtons();
        }

        private void UpdateLoginTypeButtons()
        {
            // 通过按钮背景色来显示当前选中的登录类型
            if (_selectedLoginType == LoginType.Microsoft)
            {
                PremiumLoginButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                OfflineLoginButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
            }
            else
            {
                PremiumLoginButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                OfflineLoginButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
            }
        }

        private void PremiumLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("LaunchPage", "用户切换到正版登录");
            _selectedLoginType = LoginType.Microsoft;
            PremiumLoginPanel.Visibility = Visibility.Visible;
            OfflineLoginPanel.Visibility = Visibility.Collapsed;
            UpdateLoginTypeButtons();
        }

        private void OfflineLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("LaunchPage", "用户切换到离线登录");
            _selectedLoginType = LoginType.Offline;
            PremiumLoginPanel.Visibility = Visibility.Collapsed;
            OfflineLoginPanel.Visibility = Visibility.Visible;
            UpdateLoginTypeButtons();
        }

        private async void PremiumConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("LaunchPage", "用户点击正版登录按钮");
            try
            {
                var microsoftAuthService = new MicrosoftAuthService();
                var minecraftAuthService = new MinecraftAuthService();
                
                // 1. 获取设备代码
                var deviceCodeResult = await microsoftAuthService.GetDeviceCodeAsync();
                
                // 2. 显示设备代码对话框
                var dialog = new Window
                {
                    Title = "Microsoft 登录",
                    Width = 450,
                    Height = 280,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.ToolWindow,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 44))
                };

                var grid = new Grid { Margin = new Thickness(20) };
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

                var codeBorder = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(15),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var codeBlock = new TextBlock
                {
                    Text = deviceCodeResult.UserCode,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                codeBorder.Child = codeBlock;
                Grid.SetRow(codeBorder, 1);

                // 复制按钮
                var copyButton = new Button
                {
                    Content = "已自动复制",
                    Height = 32,
                    Width = 120,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    BorderThickness = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                copyButton.Click += (s, ev) =>
                {
                    Clipboard.SetText(deviceCodeResult.UserCode);
                    copyButton.Content = "已复制!";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(deviceCodeResult.VerificationUrl) { UseShellExecute = true });
                };
                Grid.SetRow(copyButton, 2);

                // 自动复制设备码到剪贴板
                Clipboard.SetText(deviceCodeResult.UserCode);
                LogService.Instance.WriteInfo("LaunchPage", "设备码已自动复制到剪贴板");

                var descBlock = new TextBlock
                {
                    Text = $"或访问: {deviceCodeResult.VerificationUrl}",
                    Foreground = System.Windows.Media.Brushes.LightBlue,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextDecorations = TextDecorations.Underline,
                    Cursor = Cursors.Hand
                };
                descBlock.MouseLeftButtonDown += (s, ev) =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(deviceCodeResult.VerificationUrl) { UseShellExecute = true });
                };
                Grid.SetRow(descBlock, 3);

                grid.Children.Add(titleBlock);
                grid.Children.Add(codeBorder);
                grid.Children.Add(copyButton);
                grid.Children.Add(descBlock);

                dialog.Content = grid;
                dialog.Show();

                // 自动打开浏览器
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(deviceCodeResult.VerificationUrl) { UseShellExecute = true });

                // 3. 轮询获取 token
                var microsoftToken = await microsoftAuthService.GetTokenAsync(
                    deviceCodeResult.DeviceCode,
                    deviceCodeResult.Interval,
                    deviceCodeResult.ExpiresIn
                );

                dialog.Close();

                // 4. 进行 Minecraft 认证
                var minecraftAuth = await minecraftAuthService.AuthenticateAsync(microsoftToken.AccessToken);

                if (minecraftAuth != null && minecraftAuth.IsSuccessful)
                {
                    _currentUser = minecraftAuth;
                    LogService.Instance.WriteInfo("LaunchPage", $"正版登录成功: {minecraftAuth.Username}");
                    UpdateUI();
                    MessageBox.Show($"登录成功！\n用户名: {minecraftAuth.Username}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LogService.Instance.WriteWarning("LaunchPage", "Minecraft 认证失败");
                    MessageBox.Show("Minecraft 认证失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("LaunchPage", "正版登录发生异常", ex);
                MessageBox.Show($"登录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OfflineConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string username = OfflineUsernameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(username))
            {
                LogService.Instance.WriteWarning("LaunchPage", "离线登录失败：用户名为空");
                MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LogService.Instance.WriteInfo("LaunchPage", $"离线登录成功: {username}");
            _currentUser = new AuthResult
            {
                Username = username,
                LoginType = LoginType.Offline
            };
            UpdateUI();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null)
            {
                LogService.Instance.WriteInfo("LaunchPage", $"用户退出登录: {_currentUser.Username}");
            }
            _currentUser = null;
            _selectedLoginType = LoginType.Microsoft;
            PremiumLoginPanel.Visibility = Visibility.Visible;
            OfflineLoginPanel.Visibility = Visibility.Collapsed;
            OfflineUsernameTextBox.Clear();
            LoggedInInfoPanel.Visibility = Visibility.Collapsed;
            if (RightContentPanel != null)
                RightContentPanel.Visibility = Visibility.Collapsed;
            PremiumPlayerAvatar.Visibility = Visibility.Collapsed;
            PremiumPlayerNameText.Visibility = Visibility.Collapsed;
            PremiumConfirmButton.Visibility = Visibility.Visible;
            UpdateLoginTypeButtons();
        }

        private void LaunchGameButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("LaunchPage", "用户点击了启动游戏按钮");
            if (_currentUser == null)
            {
                LogService.Instance.WriteWarning("LaunchPage", "启动游戏失败：用户未登录");
                MessageBox.Show("请先选择登录方式", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string loginType = _currentUser.LoginType == LoginType.Microsoft ? "正版" : "离线";
            LogService.Instance.WriteInfo("LaunchPage", $"启动游戏：用户名: {_currentUser.Username}, 登录方式: {loginType}");
            MessageBox.Show($"启动游戏功能开发中...\n用户名: {_currentUser.Username}\n登录方式: {loginType}",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
