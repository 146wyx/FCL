using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace FunCraftLauncher.Services
{
    /// <summary>
    /// 下载管理器 - 处理游戏下载的完整流程
    /// </summary>
    public class DownloadManager
    {
        private readonly GameDownloadService _gameDownloadService;
        private readonly ModLoaderService _modLoaderService;
        private readonly string _gameDirectory;

        public event Action<string, int, int>? OnProgressChanged; // 文件路径, 当前进度, 总进度
        public event Action<string>? OnStatusChanged; // 状态消息
        public event Action<bool, string>? OnCompleted; // 是否成功, 消息

        public DownloadManager(string gameDirectory)
        {
            _gameDirectory = gameDirectory;
            _gameDownloadService = new GameDownloadService(gameDirectory);
            _modLoaderService = new ModLoaderService();
        }

        /// <summary>
        /// 下载游戏（原版 + Mod 加载器）
        /// </summary>
        public async Task DownloadGameAsync(string versionId, string versionUrl, string modLoader, string? modLoaderVersion = null)
        {
            try
            {
                OnStatusChanged?.Invoke($"准备下载 Minecraft {versionId}...");
                LogService.Instance.WriteInfo("DownloadManager", $"开始下载游戏 - 版本: {versionId}, Mod加载器: {modLoader}, Mod版本: {modLoaderVersion}");

                // 1. 下载原版游戏
                var success = await DownloadVanillaGameAsync(versionId, versionUrl);
                if (!success)
                {
                    OnCompleted?.Invoke(false, "原版游戏下载失败");
                    return;
                }

                // 2. 根据 Mod 加载器类型下载
                if (modLoader != "none" && !string.IsNullOrEmpty(modLoaderVersion))
                {
                    OnStatusChanged?.Invoke($"正在下载 {modLoader} {modLoaderVersion}...");
                    
                    switch (modLoader)
                    {
                        case "forge":
                            success = await DownloadForgeAsync(versionId, modLoaderVersion);
                            break;
                        case "neoforge":
                            success = await DownloadNeoForgeAsync(versionId, modLoaderVersion);
                            break;
                        case "fabric":
                            success = await DownloadFabricAsync(versionId);
                            break;
                        case "quilt":
                            success = await DownloadQuiltAsync(versionId);
                            break;
                    }

                    if (!success)
                    {
                        OnCompleted?.Invoke(false, $"{modLoader} 下载失败");
                        return;
                    }
                }

                OnCompleted?.Invoke(true, $"Minecraft {versionId} 下载完成！");
                LogService.Instance.WriteInfo("DownloadManager", $"游戏下载完成: {versionId}");
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("DownloadManager", "下载游戏时发生异常", ex);
                OnCompleted?.Invoke(false, $"下载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 下载原版游戏
        /// </summary>
        private async Task<bool> DownloadVanillaGameAsync(string versionId, string versionUrl)
        {
            try
            {
                OnStatusChanged?.Invoke($"正在获取版本信息...");
                
                // 获取版本详情
                var versionDetail = await _gameDownloadService.GetVersionDetailAsync(versionUrl);
                if (versionDetail == null)
                {
                    LogService.Instance.WriteError("DownloadManager", $"获取版本详情失败: {versionId}");
                    return false;
                }

                // 下载版本 JSON
                OnStatusChanged?.Invoke($"正在下载版本配置...");
                var jsonSuccess = await _gameDownloadService.DownloadVersionJsonAsync(versionId, versionUrl);
                if (!jsonSuccess)
                {
                    return false;
                }

                // 下载客户端 Jar
                OnStatusChanged?.Invoke($"正在下载游戏核心...");
                if (versionDetail.Downloads?.Client != null)
                {
                    var client = versionDetail.Downloads.Client;
                    var jarSuccess = await _gameDownloadService.DownloadClientJarAsync(
                        versionId, client.Url, client.Sha1);
                    if (!jarSuccess)
                    {
                        return false;
                    }
                }

                // TODO: 下载资源文件和库文件
                OnStatusChanged?.Invoke($"正在准备资源文件...");
                // await DownloadAssetsAsync(versionDetail);
                // await DownloadLibrariesAsync(versionDetail);

                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("DownloadManager", $"下载原版游戏失败: {versionId}", ex);
                return false;
            }
        }

        /// <summary>
        /// 下载 Forge
        /// </summary>
        private async Task<bool> DownloadForgeAsync(string mcVersion, string forgeVersion)
        {
            try
            {
                OnStatusChanged?.Invoke($"正在下载 Forge {forgeVersion}...");
                
                var downloadUrl = _modLoaderService.BuildForgeDownloadUrl(mcVersion, forgeVersion, "installer", "jar");
                var installerPath = Path.Combine(_gameDirectory, "versions", mcVersion, $"forge-{mcVersion}-{forgeVersion}-installer.jar");

                // 下载 Forge 安装器
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(installerPath, bytes);

                LogService.Instance.WriteInfo("DownloadManager", $"Forge 安装器下载完成: {installerPath}");
                
                // TODO: 运行 Forge 安装器
                OnStatusChanged?.Invoke($"Forge 安装器已下载，请手动运行安装");
                
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("DownloadManager", $"下载 Forge 失败: {forgeVersion}", ex);
                return false;
            }
        }

        /// <summary>
        /// 下载 NeoForge
        /// </summary>
        private async Task<bool> DownloadNeoForgeAsync(string mcVersion, string neoForgeVersion)
        {
            try
            {
                OnStatusChanged?.Invoke($"正在下载 NeoForge {neoForgeVersion}...");
                
                var downloadUrl = _modLoaderService.BuildNeoForgeDownloadUrl(mcVersion, neoForgeVersion);
                var installerPath = Path.Combine(_gameDirectory, "versions", mcVersion, $"neoforge-{mcVersion}-{neoForgeVersion}-installer.jar");

                // 下载 NeoForge 安装器
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(installerPath, bytes);

                LogService.Instance.WriteInfo("DownloadManager", $"NeoForge 安装器下载完成: {installerPath}");
                
                OnStatusChanged?.Invoke($"NeoForge 安装器已下载，请手动运行安装");
                
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("DownloadManager", $"下载 NeoForge 失败: {neoForgeVersion}", ex);
                return false;
            }
        }

        /// <summary>
        /// 下载 Fabric
        /// </summary>
        private async Task<bool> DownloadFabricAsync(string mcVersion)
        {
            try
            {
                OnStatusChanged?.Invoke($"正在下载 Fabric...");
                
                // Fabric 需要通过 Fabric API 获取安装器
                // https://meta.fabricmc.net/v2/versions/installer
                var fabricUrl = $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}";
                
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(fabricUrl);
                
                LogService.Instance.WriteInfo("DownloadManager", $"Fabric 信息获取完成: {mcVersion}");
                OnStatusChanged?.Invoke($"Fabric 信息已获取，请使用 Fabric 安装器");
                
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("DownloadManager", $"下载 Fabric 失败: {mcVersion}", ex);
                return false;
            }
        }

        /// <summary>
        /// 下载 Quilt
        /// </summary>
        private async Task<bool> DownloadQuiltAsync(string mcVersion)
        {
            try
            {
                OnStatusChanged?.Invoke($"正在下载 Quilt...");
                
                // Quilt 需要通过 Quilt API 获取安装器
                var quiltUrl = $"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}";
                
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(quiltUrl);
                
                LogService.Instance.WriteInfo("DownloadManager", $"Quilt 信息获取完成: {mcVersion}");
                OnStatusChanged?.Invoke($"Quilt 信息已获取，请使用 Quilt 安装器");
                
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("DownloadManager", $"下载 Quilt 失败: {mcVersion}", ex);
                return false;
            }
        }
    }
}
