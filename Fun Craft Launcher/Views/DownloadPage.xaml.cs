using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Views
{
    public partial class DownloadPage : UserControl
    {
        private List<MinecraftVersion> _allVersions = new();
        private List<MinecraftVersion> _filteredVersions = new();
        private readonly HttpClient _httpClient;
        private string _currentFilter = "release";
        private string _searchText = "";
        private bool _isLoaded = false;

        public DownloadPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            Loaded += DownloadPage_Loaded;
            IsVisibleChanged += DownloadPage_IsVisibleChanged;
        }

        private async void DownloadPage_Loaded(object sender, RoutedEventArgs e)
        {
            LogService.Instance.WriteInfo("DownloadPage", "页面已加载");
            // 页面加载时也尝试加载版本列表
            if (!_isLoaded)
            {
                LogService.Instance.WriteInfo("DownloadPage", "页面加载完成，开始加载版本列表");
                _isLoaded = true;
                await LoadVersionsAsync();
            }
        }

        private async void DownloadPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && !_isLoaded)
            {
                LogService.Instance.WriteInfo("DownloadPage", "页面变为可见，开始加载版本列表");
                _isLoaded = true;
                await LoadVersionsAsync();
            }
        }

        private async Task LoadVersionsAsync()
        {
            const int maxRetries = 3;
            int retryCount = 0;
            Exception? lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    ShowLoading(true);
                    LogService.Instance.WriteInfo("DownloadPage", $"开始加载 Minecraft 版本列表 (尝试 {retryCount + 1}/{maxRetries})");

                    // 使用 BMCLAPI 国内镜像源，添加超时设置
                    const string manifestUrl = "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json";
                    
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var response = await _httpClient.GetStringAsync(manifestUrl, cts.Token);
                    LogService.Instance.WriteInfo("DownloadPage", $"API 响应长度: {response.Length}");

                    // 配置 JSON 反序列化选项，忽略大小写
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var manifest = JsonSerializer.Deserialize<VersionManifest>(response, options);

                    if (manifest?.Versions != null && manifest.Versions.Count > 0)
                    {
                        _allVersions = manifest.Versions.Select(v => new MinecraftVersion
                        {
                            Id = v.Id ?? "",
                            Type = v.Type ?? "",
                            ReleaseTime = DateTime.TryParse(v.ReleaseTime, out var date) ? date : DateTime.MinValue,
                            Url = v.Url ?? ""
                        }).ToList();

                        LogService.Instance.WriteInfo("DownloadPage", $"成功加载 {_allVersions.Count} 个版本");
                        ShowLoading(false);
                        ApplyFilters();
                        return; // 成功加载，退出方法
                    }
                    else
                    {
                        LogService.Instance.WriteWarning("DownloadPage", $"版本清单解析失败或为空, manifest null: {manifest == null}, versions null: {manifest?.Versions == null}, count: {manifest?.Versions?.Count ?? 0}");
                        ShowLoading(false);
                        ShowEmpty(true);
                        return;
                    }
                }
                catch (System.Net.Http.HttpRequestException httpEx)
                {
                    lastException = httpEx;
                    retryCount++;
                    LogService.Instance.WriteWarning("DownloadPage", $"HTTP 请求失败 (尝试 {retryCount}/{maxRetries}): {httpEx.Message}");
                    
                    if (retryCount < maxRetries)
                    {
                        LogService.Instance.WriteInfo("DownloadPage", $"等待 2 秒后重试...");
                        await Task.Delay(2000);
                    }
                }
                catch (System.Threading.Tasks.TaskCanceledException cancelEx)
                {
                    lastException = cancelEx;
                    retryCount++;
                    LogService.Instance.WriteWarning("DownloadPage", $"请求超时 (尝试 {retryCount}/{maxRetries})");
                    
                    if (retryCount < maxRetries)
                    {
                        LogService.Instance.WriteInfo("DownloadPage", $"等待 2 秒后重试...");
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    LogService.Instance.WriteError("DownloadPage", $"加载版本列表失败 (尝试 {retryCount}/{maxRetries})", ex);
                    
                    if (retryCount < maxRetries)
                    {
                        LogService.Instance.WriteInfo("DownloadPage", $"等待 2 秒后重试...");
                        await Task.Delay(2000);
                    }
                }
                finally
                {
                    if (retryCount >= maxRetries)
                    {
                        ShowLoading(false);
                    }
                }
            }

            // 所有重试都失败了
            LogService.Instance.WriteError("DownloadPage", $"加载版本列表失败，已重试 {maxRetries} 次", lastException);
            
            // 在主线程上显示错误提示
            Dispatcher.Invoke(() =>
            {
                ShowEmpty(true);
                MessageBox.Show(
                    $"加载版本列表失败，请检查网络连接后重试。\n\n错误信息：{lastException?.Message}",
                    "网络错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }

        private void ApplyFilters()
        {
            IEnumerable<MinecraftVersion> filtered = _allVersions;

            filtered = _currentFilter switch
            {
                "release" => filtered.Where(v => v.Type == "release"),
                "snapshot" => filtered.Where(v => v.Type == "snapshot"),
                "beta" => filtered.Where(v => v.Type == "old_beta"),
                "alpha" => filtered.Where(v => v.Type == "old_alpha"),
                _ => filtered
            };

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(v => v.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            }

            _filteredVersions = filtered.ToList();
            RenderVersionList();
        }

        private void RenderVersionList()
        {
            if (VersionListPanel == null) return;

            VersionListPanel.Children.Clear();

            if (_filteredVersions.Count == 0)
            {
                ShowEmpty(true);
                return;
            }

            ShowEmpty(false);

            foreach (var version in _filteredVersions)
            {
                var item = CreateVersionItem(version);
                VersionListPanel.Children.Add(item);
            }
        }

        private Border CreateVersionItem(MinecraftVersion version)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            var versionText = new TextBlock
            {
                Text = version.Id,
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(versionText, 0);

            var typeBorder = new Border
            {
                Background = GetTypeColor(version.Type),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var typeText = new TextBlock
            {
                Text = GetTypeDisplayName(version.Type),
                Foreground = Brushes.White,
                FontSize = 11
            };
            typeBorder.Child = typeText;
            Grid.SetColumn(typeBorder, 1);

            var dateText = new TextBlock
            {
                Text = version.ReleaseTime.ToString("yyyy-MM-dd"),
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 2);

            var downloadButton = new Button
            {
                Content = "下载",
                Height = 28,
                Width = 70,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                Foreground = Brushes.White,
                FontSize = 12,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = version
            };
            downloadButton.Click += DownloadButton_Click;
            
            var buttonStyle = new Style(typeof(Button));
            var templateSetter = new Setter(Control.TemplateProperty, CreateButtonTemplate());
            buttonStyle.Setters.Add(templateSetter);
            
            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(16, 110, 190))));
            buttonStyle.Triggers.Add(trigger);
            
            downloadButton.Style = buttonStyle;
            Grid.SetColumn(downloadButton, 3);

            grid.Children.Add(versionText);
            grid.Children.Add(typeBorder);
            grid.Children.Add(dateText);
            grid.Children.Add(downloadButton);

            border.Child = grid;
            return border;
        }

        private ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            
            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;
            
            return template;
        }

        private static string GetTypeDisplayName(string type)
        {
            return type switch
            {
                "release" => "正式版",
                "snapshot" => "快照版",
                "old_beta" => "Beta",
                "old_alpha" => "Alpha",
                _ => type
            };
        }

        private static Brush GetTypeColor(string type)
        {
            var color = type switch
            {
                "release" => Color.FromRgb(76, 175, 80),
                "snapshot" => Color.FromRgb(255, 152, 0),
                "old_beta" => Color.FromRgb(156, 39, 176),
                "old_alpha" => Color.FromRgb(96, 125, 139),
                _ => Color.FromRgb(117, 117, 117)
            };
            return new SolidColorBrush(color);
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MinecraftVersion version)
            {
                LogService.Instance.WriteInfo("DownloadPage", $"用户点击下载版本: {version.Id}");
                
                // 打开下载选项对话框
                var dialog = new DownloadOptionsDialog(version.Id, version.Type);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    // 用户确认了下载，开始实际下载
                    string modLoaderText = dialog.SelectedModLoader switch
                    {
                        "none" => "不安装 Mod 加载器",
                        "forge" => "Forge",
                        "neoforge" => "NeoForge",
                        "fabric" => "Fabric",
                        "quilt" => "Quilt",
                        _ => "未知"
                    };
                    
                    LogService.Instance.WriteInfo("DownloadPage", 
                        $"开始下载 - 版本: {dialog.SelectedVersion}, Mod加载器: {modLoaderText}, Mod版本: {dialog.SelectedModLoaderVersion}");
                    
                    // 创建下载管理器，使用启动器所在目录下的 .minecraft 文件夹
                    var launcherDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var gameDir = Path.Combine(launcherDir!, ".minecraft");
                    var downloadManager = new DownloadManager(gameDir);
                    
                    // 订阅下载事件
                    downloadManager.OnStatusChanged += (status) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LogService.Instance.WriteInfo("DownloadPage", $"下载状态: {status}");
                        });
                    };
                    
                    downloadManager.OnCompleted += (success, message) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (success)
                            {
                                MessageBox.Show(message, "下载完成", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show(message, "下载失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    };
                    
                    // 开始下载
                    await downloadManager.DownloadGameAsync(
                        dialog.SelectedVersion, 
                        version.Url, 
                        dialog.SelectedModLoader, 
                        dialog.SelectedModLoaderVersion);
                }
            }
        }

        private void VersionType_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio)
            {
                _currentFilter = radio.Name switch
                {
                    "ReleaseRadio" => "release",
                    "SnapshotRadio" => "snapshot",
                    "BetaRadio" => "beta",
                    "AlphaRadio" => "alpha",
                    "AllRadio" => "all",
                    _ => "release"
                };
                ApplyFilters();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox?.Text?.Trim() ?? "";
            ApplyFilters();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadVersionsAsync();
        }

        private void ShowLoading(bool show)
        {
            if (LoadingPanel != null)
                LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowEmpty(bool show)
        {
            if (EmptyPanel != null)
                EmptyPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class MinecraftVersion
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime ReleaseTime { get; set; }
        public string Url { get; set; } = "";
    }

    public class VersionManifest
    {
        public LatestInfo? Latest { get; set; }
        public List<VersionInfo>? Versions { get; set; }
    }

    public class LatestInfo
    {
        public string? Release { get; set; }
        public string? Snapshot { get; set; }
    }

    public class VersionInfo
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Url { get; set; }
        public string? Time { get; set; }
        public string? ReleaseTime { get; set; }
    }
}
