using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FunCraftLauncher.Services
{
    /// <summary>
    /// 全局设置服务 - 管理启动器的全局设置
    /// </summary>
    public class GlobalSettingsService
    {
        private readonly string _settingsFilePath;
        private GlobalSettings _settings;

        public GlobalSettingsService()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FunCraftLauncher");
            Directory.CreateDirectory(appDataPath);
            _settingsFilePath = Path.Combine(appDataPath, "settings.json");
            _settings = LoadSettings();
        }

        /// <summary>
        /// 获取当前设置
        /// </summary>
        public GlobalSettings Settings => _settings;

        /// <summary>
        /// 加载设置
        /// </summary>
        private GlobalSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<GlobalSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("GlobalSettingsService", "加载设置失败", ex);
            }

            return new GlobalSettings();
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsFilePath, json);
                LogService.Instance.WriteInfo("GlobalSettingsService", "设置已保存");
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("GlobalSettingsService", "保存设置失败", ex);
            }
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public void ResetToDefault()
        {
            _settings = new GlobalSettings();
            SaveSettings();
        }
    }

    /// <summary>
    /// 全局设置数据
    /// </summary>
    public class GlobalSettings
    {
        // Java设置
        public JavaSettings Java { get; set; } = new JavaSettings();

        // 游戏设置
        public GameSettings Game { get; set; } = new GameSettings();

        // 下载设置
        public DownloadSettings Download { get; set; } = new DownloadSettings();

        // 启动器设置
        public LauncherSettings Launcher { get; set; } = new LauncherSettings();

        // 外观设置
        public AppearanceSettings Appearance { get; set; } = new AppearanceSettings();
    }

    /// <summary>
    /// Java设置
    /// </summary>
    public class JavaSettings
    {
        /// <summary>
        /// 自动查找Java
        /// </summary>
        public bool AutoFindJava { get; set; } = true;

        /// <summary>
        /// 自定义Java路径
        /// </summary>
        public string? CustomJavaPath { get; set; }

        /// <summary>
        /// 最大内存(MB)
        /// </summary>
        public int MaxMemory { get; set; } = 2048;

        /// <summary>
        /// 最小内存(MB)
        /// </summary>
        public int MinMemory { get; set; } = 512;

        /// <summary>
        /// 自定义JVM参数
        /// </summary>
        public string? CustomJvmArgs { get; set; }
    }

    /// <summary>
    /// 游戏设置
    /// </summary>
    public class GameSettings
    {
        /// <summary>
        /// 默认游戏目录
        /// </summary>
        public string? DefaultGameDirectory { get; set; }

        /// <summary>
        /// 默认窗口宽度
        /// </summary>
        public int WindowWidth { get; set; } = 854;

        /// <summary>
        /// 默认窗口高度
        /// </summary>
        public int WindowHeight { get; set; } = 480;

        /// <summary>
        /// 默认全屏
        /// </summary>
        public bool FullScreen { get; set; } = false;

        /// <summary>
        /// 启动前检查文件完整性
        /// </summary>
        public bool CheckFileIntegrity { get; set; } = true;

        /// <summary>
        /// 自动补全缺失文件
        /// </summary>
        public bool AutoCompleteFiles { get; set; } = true;
    }

    /// <summary>
    /// 下载设置
    /// </summary>
    public class DownloadSettings
    {
        /// <summary>
        /// 下载源
        /// </summary>
        public string DownloadSource { get; set; } = "bmclapi";

        /// <summary>
        /// 并发下载数
        /// </summary>
        public int ConcurrentDownloads { get; set; } = 64;

        /// <summary>
        /// 下载超时(秒)
        /// </summary>
        public int DownloadTimeout { get; set; } = 60;

        /// <summary>
        /// 下载后自动安装
        /// </summary>
        public bool AutoInstall { get; set; } = true;

        /// <summary>
        /// 保留安装包
        /// </summary>
        public bool KeepInstaller { get; set; } = false;
    }

    /// <summary>
    /// 启动器设置
    /// </summary>
    public class LauncherSettings
    {
        /// <summary>
        /// 启动后关闭启动器
        /// </summary>
        public bool CloseAfterLaunch { get; set; } = false;

        /// <summary>
        /// 显示游戏输出窗口
        /// </summary>
        public bool ShowGameOutput { get; set; } = false;

        /// <summary>
        /// 最小化到托盘
        /// </summary>
        public bool MinimizeToTray { get; set; } = false;

        /// <summary>
        /// 自动登录
        /// </summary>
        public bool AutoLogin { get; set; } = false;

        /// <summary>
        /// 记住密码
        /// </summary>
        public bool RememberPassword { get; set; } = true;

        /// <summary>
        /// 启动时检查更新
        /// </summary>
        public bool CheckUpdateOnStart { get; set; } = true;

        /// <summary>
        /// 日志级别
        /// </summary>
        public string LogLevel { get; set; } = "Info";
    }

    /// <summary>
    /// 外观设置
    /// </summary>
    public class AppearanceSettings
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>
        /// 语言
        /// </summary>
        public string Language { get; set; } = "zh-CN";

        /// <summary>
        /// 背景图片
        /// </summary>
        public string? BackgroundImage { get; set; }

        /// <summary>
        /// 背景透明度
        /// </summary>
        public double BackgroundOpacity { get; set; } = 1.0;

        /// <summary>
        /// 启用动画
        /// </summary>
        public bool EnableAnimations { get; set; } = true;

        /// <summary>
        /// 字体大小
        /// </summary>
        public int FontSize { get; set; } = 14;
    }
}
