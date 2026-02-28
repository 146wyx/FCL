using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LogService.Instance.WriteInfo("MainWindow", "应用程序已启动");
            
            // 自动创建 .minecraft 文件夹
            InitializeGameDirectory();
        }
        
        /// <summary>
        /// 初始化游戏目录，在启动器所在文件夹下自动创建 .minecraft 文件夹
        /// </summary>
        private void InitializeGameDirectory()
        {
            try
            {
                // 获取启动器所在目录（使用 AppContext.BaseDirectory 兼容单文件发布）
                var launcherDir = AppContext.BaseDirectory;
                var gameDir = Path.Combine(launcherDir, ".minecraft");
                
                if (!Directory.Exists(gameDir))
                {
                    Directory.CreateDirectory(gameDir);
                    LogService.Instance.WriteInfo("MainWindow", $"创建游戏目录: {gameDir}");
                    
                    // 创建必要的子目录
                    Directory.CreateDirectory(Path.Combine(gameDir, "versions"));
                    Directory.CreateDirectory(Path.Combine(gameDir, "libraries"));
                    Directory.CreateDirectory(Path.Combine(gameDir, "assets"));
                    Directory.CreateDirectory(Path.Combine(gameDir, "mods"));
                    Directory.CreateDirectory(Path.Combine(gameDir, "resourcepacks"));
                    Directory.CreateDirectory(Path.Combine(gameDir, "saves"));
                    Directory.CreateDirectory(Path.Combine(gameDir, "screenshots"));
                    
                    LogService.Instance.WriteInfo("MainWindow", "游戏子目录创建完成");
                }
                else
                {
                    LogService.Instance.WriteInfo("MainWindow", $"游戏目录已存在: {gameDir}");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("MainWindow", "创建游戏目录失败", ex);
            }
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

        #region 导航按钮事件

        private void LaunchNavButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("MainWindow", "用户切换到启动页面");
            ShowPage("launch");
        }

        private void DownloadNavButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("MainWindow", "用户切换到下载页面");
            ShowPage("download");
        }

        private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("MainWindow", "用户点击设置按钮");
            ShowPage("settings");
        }

        private void ShowPage(string page)
        {
            // 重置所有导航按钮样式
            LaunchNavButton.Background = System.Windows.Media.Brushes.Transparent;
            LaunchNavButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));
            DownloadNavButton.Background = System.Windows.Media.Brushes.Transparent;
            DownloadNavButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));
            SettingsNavButton.Background = System.Windows.Media.Brushes.Transparent;
            SettingsNavButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));

            // 隐藏所有页面
            LaunchContentPanel.Visibility = Visibility.Collapsed;
            DownloadContentPanel.Visibility = Visibility.Collapsed;
            SettingsContentPanel.Visibility = Visibility.Collapsed;

            // 显示选中的页面
            switch (page)
            {
                case "launch":
                    LaunchContentPanel.Visibility = Visibility.Visible;
                    LaunchNavButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                    LaunchNavButton.Foreground = System.Windows.Media.Brushes.White;
                    break;
                case "download":
                    DownloadContentPanel.Visibility = Visibility.Visible;
                    DownloadNavButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                    DownloadNavButton.Foreground = System.Windows.Media.Brushes.White;
                    break;
                case "settings":
                    SettingsContentPanel.Visibility = Visibility.Visible;
                    SettingsNavButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                    SettingsNavButton.Foreground = System.Windows.Media.Brushes.White;
                    break;
            }
        }

        #endregion

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

        private void LaunchContentPanel_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
