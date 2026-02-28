using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Views
{
    public partial class SettingsPage : UserControl
    {
        private readonly GlobalSettingsService _settingsService;
        private GlobalSettings _settings;

        public SettingsPage()
        {
            InitializeComponent();
            _settingsService = new GlobalSettingsService();
            _settings = _settingsService.Settings;
            LoadSettings();
        }

        /// <summary>
        /// 加载设置到UI
        /// </summary>
        private void LoadSettings()
        {
            // Java设置
            AutoFindJavaCheck.IsChecked = _settings.Java.AutoFindJava;
            JavaPathTextBox.Text = _settings.Java.CustomJavaPath ?? "";
            MaxMemorySlider.Value = _settings.Java.MaxMemory;
            MaxMemoryText.Text = $"{_settings.Java.MaxMemory} MB";
            MinMemorySlider.Value = _settings.Java.MinMemory;
            MinMemoryText.Text = $"{_settings.Java.MinMemory} MB";
            JvmArgsTextBox.Text = _settings.Java.CustomJvmArgs ?? "";

            // 游戏设置
            GameDirectoryTextBox.Text = _settings.Game.DefaultGameDirectory ?? "";
            FullScreenCheck.IsChecked = _settings.Game.FullScreen;
            WindowWidthTextBox.Text = _settings.Game.WindowWidth.ToString();
            WindowHeightTextBox.Text = _settings.Game.WindowHeight.ToString();
            CheckIntegrityCheck.IsChecked = _settings.Game.CheckFileIntegrity;
            AutoCompleteCheck.IsChecked = _settings.Game.AutoCompleteFiles;
            CloseLauncherCheck.IsChecked = _settings.Launcher.CloseAfterLaunch;
            ShowOutputCheck.IsChecked = _settings.Launcher.ShowGameOutput;

            // 下载设置
            DownloadSourceComboBox.SelectedIndex = _settings.Download.DownloadSource == "bmclapi" ? 0 : 1;
            ConcurrentDownloadsSlider.Value = _settings.Download.ConcurrentDownloads;
            ConcurrentDownloadsText.Text = _settings.Download.ConcurrentDownloads.ToString();
            DownloadTimeoutSlider.Value = _settings.Download.DownloadTimeout;
            DownloadTimeoutText.Text = $"{_settings.Download.DownloadTimeout}秒";
            AutoInstallCheck.IsChecked = _settings.Download.AutoInstall;
            KeepInstallerCheck.IsChecked = _settings.Download.KeepInstaller;

            // 外观设置
            ThemeComboBox.SelectedIndex = _settings.Appearance.Theme == "Dark" ? 0 : 1;
            LanguageComboBox.SelectedIndex = _settings.Appearance.Language switch
            {
                "zh-CN" => 0,
                "zh-TW" => 1,
                "en-US" => 2,
                _ => 0
            };
            EnableAnimationsCheck.IsChecked = _settings.Appearance.EnableAnimations;
            FontSizeSlider.Value = _settings.Appearance.FontSize;
            FontSizeText.Text = _settings.Appearance.FontSize.ToString();
        }

        /// <summary>
        /// 从UI保存设置
        /// </summary>
        private void SaveSettingsFromUI()
        {
            // Java设置
            _settings.Java.AutoFindJava = AutoFindJavaCheck.IsChecked == true;
            _settings.Java.CustomJavaPath = string.IsNullOrWhiteSpace(JavaPathTextBox.Text) ? null : JavaPathTextBox.Text;
            _settings.Java.MaxMemory = (int)MaxMemorySlider.Value;
            _settings.Java.MinMemory = (int)MinMemorySlider.Value;
            _settings.Java.CustomJvmArgs = string.IsNullOrWhiteSpace(JvmArgsTextBox.Text) ? null : JvmArgsTextBox.Text;

            // 游戏设置
            _settings.Game.DefaultGameDirectory = string.IsNullOrWhiteSpace(GameDirectoryTextBox.Text) ? null : GameDirectoryTextBox.Text;
            _settings.Game.FullScreen = FullScreenCheck.IsChecked == true;
            if (int.TryParse(WindowWidthTextBox.Text, out var width))
                _settings.Game.WindowWidth = width;
            if (int.TryParse(WindowHeightTextBox.Text, out var height))
                _settings.Game.WindowHeight = height;
            _settings.Game.CheckFileIntegrity = CheckIntegrityCheck.IsChecked == true;
            _settings.Game.AutoCompleteFiles = AutoCompleteCheck.IsChecked == true;

            // 启动器设置
            _settings.Launcher.CloseAfterLaunch = CloseLauncherCheck.IsChecked == true;
            _settings.Launcher.ShowGameOutput = ShowOutputCheck.IsChecked == true;

            // 下载设置
            _settings.Download.DownloadSource = DownloadSourceComboBox.SelectedIndex == 0 ? "bmclapi" : "mojang";
            _settings.Download.ConcurrentDownloads = (int)ConcurrentDownloadsSlider.Value;
            _settings.Download.DownloadTimeout = (int)DownloadTimeoutSlider.Value;
            _settings.Download.AutoInstall = AutoInstallCheck.IsChecked == true;
            _settings.Download.KeepInstaller = KeepInstallerCheck.IsChecked == true;

            // 外观设置
            _settings.Appearance.Theme = ThemeComboBox.SelectedIndex == 0 ? "Dark" : "Light";
            _settings.Appearance.Language = LanguageComboBox.SelectedIndex switch
            {
                0 => "zh-CN",
                1 => "zh-TW",
                2 => "en-US",
                _ => "zh-CN"
            };
            _settings.Appearance.EnableAnimations = EnableAnimationsCheck.IsChecked == true;
            _settings.Appearance.FontSize = (int)FontSizeSlider.Value;

            _settingsService.SaveSettings();
        }

        #region 事件处理

        private void BrowseJavaButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Java可执行文件|java.exe|所有文件|*.*",
                Title = "选择Java路径"
            };

            if (dialog.ShowDialog() == true)
            {
                JavaPathTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseGameDirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择游戏目录"
            };

            if (dialog.ShowDialog() == true)
            {
                GameDirectoryTextBox.Text = dialog.FolderName;
            }
        }

        private void MaxMemorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MaxMemoryText != null)
            {
                MaxMemoryText.Text = $"{(int)e.NewValue} MB";
            }
        }

        private void MinMemorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MinMemoryText != null)
            {
                MinMemoryText.Text = $"{(int)e.NewValue} MB";
            }
        }

        private void ConcurrentDownloadsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ConcurrentDownloadsText != null)
            {
                ConcurrentDownloadsText.Text = ((int)e.NewValue).ToString();
            }
        }

        private void DownloadTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DownloadTimeoutText != null)
            {
                DownloadTimeoutText.Text = $"{(int)e.NewValue}秒";
            }
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FontSizeText != null)
            {
                FontSizeText.Text = ((int)e.NewValue).ToString();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要恢复默认设置吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _settingsService.ResetToDefault();
                _settings = _settingsService.Settings;
                LoadSettings();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettingsFromUI();
                MessageBox.Show("设置已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
