using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Views
{
    public partial class DownloadOptionsDialog : Window
    {
        public string SelectedVersion { get; set; } = "";
        public string SelectedType { get; set; } = "";
        public string SelectedModLoader { get; set; } = "none";
        public string SelectedModLoaderVersion { get; set; } = "";
        public bool IsConfirmed { get; private set; } = false;

        private readonly ModLoaderService _modLoaderService;
        private List<ForgeVersion> _forgeVersions = new();
        private List<NeoForgeVersion> _neoForgeVersions = new();
        private RadioButton? _selectedVersionRadio;

        public DownloadOptionsDialog(string versionId, string versionType)
        {
            InitializeComponent();
            _modLoaderService = new ModLoaderService();
            SelectedVersion = versionId;
            SelectedType = versionType;
            
            // 设置标题和版本信息
            TitleText.Text = $"下载 Minecraft {versionId}";
            VersionText.Text = versionId;
            TypeText.Text = GetTypeDisplayName(versionType);
        }

        private static string GetTypeDisplayName(string type)
        {
            return type switch
            {
                "release" => "正式版",
                "snapshot" => "快照版",
                "old_beta" => "Beta版",
                "old_alpha" => "Alpha版",
                _ => type
            };
        }

        private async void ModLoaderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 确保控件已初始化
            if (ModLoaderVersionPanel == null || VersionSelectLabel == null || LoadingPanel == null)
            {
                LogService.Instance.WriteWarning("DownloadOptionsDialog", "控件尚未初始化，跳过处理");
                return;
            }

            if (ModLoaderComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                SelectedModLoader = tag;
                ModLoaderVersionPanel.Children.Clear();
                _selectedVersionRadio = null;
                SelectedModLoaderVersion = "";

                if (tag == "none" || tag == "fabric" || tag == "quilt")
                {
                    // Fabric 和 Quilt 不需要选择版本（暂时）
                    VersionSelectLabel.Visibility = Visibility.Collapsed;
                    return;
                }

                VersionSelectLabel.Visibility = Visibility.Visible;
                LoadingPanel.Visibility = Visibility.Visible;

                try
                {
                    if (tag == "forge")
                    {
                        await LoadForgeVersionsAsync();
                    }
                    else if (tag == "neoforge")
                    {
                        await LoadNeoForgeVersionsAsync();
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.WriteError("DownloadOptionsDialog", $"加载 {tag} 版本失败", ex);
                    MessageBox.Show($"加载版本列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async System.Threading.Tasks.Task LoadForgeVersionsAsync()
        {
            LogService.Instance.WriteInfo("DownloadOptionsDialog", $"加载 Forge 版本列表: {SelectedVersion}");
            _forgeVersions = await _modLoaderService.GetForgeVersionsAsync(SelectedVersion);

            if (_forgeVersions.Count == 0)
            {
                ModLoaderVersionPanel.Children.Add(new TextBlock
                {
                    Text = "暂无可用版本",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                });
                return;
            }

            // 按版本号排序（最新的在前）
            _forgeVersions = _forgeVersions.OrderByDescending(v => v.Build).ToList();

            foreach (var version in _forgeVersions)
            {
                var item = CreateForgeVersionItem(version);
                ModLoaderVersionPanel.Children.Add(item);
            }
        }

        private async System.Threading.Tasks.Task LoadNeoForgeVersionsAsync()
        {
            LogService.Instance.WriteInfo("DownloadOptionsDialog", $"加载 NeoForge 版本列表: {SelectedVersion}");
            _neoForgeVersions = await _modLoaderService.GetNeoForgeVersionsAsync(SelectedVersion);

            if (_neoForgeVersions.Count == 0)
            {
                ModLoaderVersionPanel.Children.Add(new TextBlock
                {
                    Text = "暂无可用版本",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                });
                return;
            }

            // 按版本号排序（最新的在前）
            _neoForgeVersions = _neoForgeVersions.OrderByDescending(v => v.Version).ToList();

            foreach (var version in _neoForgeVersions)
            {
                var item = CreateNeoForgeVersionItem(version);
                ModLoaderVersionPanel.Children.Add(item);
            }
        }

        private Border CreateForgeVersionItem(ForgeVersion version)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var radio = new RadioButton
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Tag = version.Version
            };
            radio.Checked += VersionRadio_Checked;
            Grid.SetColumn(radio, 0);

            var versionText = new TextBlock
            {
                Text = $"Forge {version.Version}",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(versionText, 1);

            grid.Children.Add(radio);
            grid.Children.Add(versionText);
            border.Child = grid;

            border.MouseLeftButtonDown += (s, e) =>
            {
                radio.IsChecked = true;
            };

            return border;
        }

        private Border CreateNeoForgeVersionItem(NeoForgeVersion version)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var radio = new RadioButton
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Tag = version.Version
            };
            radio.Checked += VersionRadio_Checked;
            Grid.SetColumn(radio, 0);

            var versionText = new TextBlock
            {
                Text = $"NeoForge {version.Version}",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(versionText, 1);

            grid.Children.Add(radio);
            grid.Children.Add(versionText);
            border.Child = grid;

            border.MouseLeftButtonDown += (s, e) =>
            {
                radio.IsChecked = true;
            };

            return border;
        }

        private void VersionRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio && radio.Tag is string version)
            {
                SelectedModLoaderVersion = version;
                LogService.Instance.WriteInfo("DownloadOptionsDialog", $"选择 {SelectedModLoader} 版本: {version}");
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证是否选择了版本（如果选择了 Forge 或 NeoForge）
            if ((SelectedModLoader == "forge" || SelectedModLoader == "neoforge") && string.IsNullOrEmpty(SelectedModLoaderVersion))
            {
                MessageBox.Show("请选择一个版本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LogService.Instance.WriteInfo("DownloadOptionsDialog", 
                $"用户确认下载 - 版本: {SelectedVersion}, Mod加载器: {SelectedModLoader}, Mod版本: {SelectedModLoaderVersion}");
            
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("DownloadOptionsDialog", "用户取消下载");
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("DownloadOptionsDialog", "用户关闭对话框");
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
