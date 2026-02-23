using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Services
{
    /// <summary>
    /// 游戏下载服务 - 处理原版游戏本体和 Mod 加载器的下载
    /// </summary>
    public class GameDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly string _gameDirectory;

        public GameDownloadService(string gameDirectory)
        {
            _httpClient = new HttpClient();
            _gameDirectory = gameDirectory;
        }

        /// <summary>
        /// 获取版本详情（包含下载链接）
        /// </summary>
        public async Task<VersionDetail?> GetVersionDetailAsync(string versionUrl)
        {
            try
            {
                LogService.Instance.WriteInfo("GameDownloadService", $"获取版本详情: {versionUrl}");
                
                var response = await _httpClient.GetStringAsync(versionUrl);
                var detail = JsonSerializer.Deserialize<VersionDetail>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return detail;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("GameDownloadService", $"获取版本详情失败: {versionUrl}", ex);
                return null;
            }
        }

        /// <summary>
        /// 下载原版游戏客户端 Jar
        /// </summary>
        public async Task<bool> DownloadClientJarAsync(string versionId, string downloadUrl, string? sha1 = null)
        {
            try
            {
                var versionsDir = Path.Combine(_gameDirectory, "versions", versionId);
                Directory.CreateDirectory(versionsDir);

                var jarPath = Path.Combine(versionsDir, $"{versionId}.jar");
                
                LogService.Instance.WriteInfo("GameDownloadService", $"下载客户端 Jar: {versionId} -> {jarPath}");
                
                // 使用 BMCLAPI 镜像替换下载链接
                var bmclUrl = ConvertToBmclDownloadUrl(downloadUrl);
                
                var bytes = await _httpClient.GetByteArrayAsync(bmclUrl);
                await File.WriteAllBytesAsync(jarPath, bytes);

                // 验证 SHA1（如果提供）
                if (!string.IsNullOrEmpty(sha1))
                {
                    var fileSha1 = await CalculateSha1Async(jarPath);
                    if (!string.Equals(fileSha1, sha1, StringComparison.OrdinalIgnoreCase))
                    {
                        LogService.Instance.WriteError("GameDownloadService", $"SHA1 验证失败: {versionId}");
                        File.Delete(jarPath);
                        return false;
                    }
                }

                LogService.Instance.WriteInfo("GameDownloadService", $"客户端 Jar 下载完成: {versionId}");
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("GameDownloadService", $"下载客户端 Jar 失败: {versionId}", ex);
                return false;
            }
        }

        /// <summary>
        /// 下载版本 JSON 文件
        /// </summary>
        public async Task<bool> DownloadVersionJsonAsync(string versionId, string versionUrl)
        {
            try
            {
                var versionsDir = Path.Combine(_gameDirectory, "versions", versionId);
                Directory.CreateDirectory(versionsDir);

                var jsonPath = Path.Combine(versionsDir, $"{versionId}.json");
                
                LogService.Instance.WriteInfo("GameDownloadService", $"下载版本 JSON: {versionId} -> {jsonPath}");
                
                // 使用 BMCLAPI 镜像
                var bmclUrl = ConvertToBmclVersionUrl(versionUrl);
                
                var json = await _httpClient.GetStringAsync(bmclUrl);
                await File.WriteAllTextAsync(jsonPath, json);

                LogService.Instance.WriteInfo("GameDownloadService", $"版本 JSON 下载完成: {versionId}");
                return true;
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("GameDownloadService", $"下载版本 JSON 失败: {versionId}", ex);
                return false;
            }
        }

        /// <summary>
        /// 将 Mojang 下载链接转换为 BMCLAPI 镜像链接
        /// </summary>
        private string ConvertToBmclDownloadUrl(string mojangUrl)
        {
            // 将 https://piston-data.mojang.com/... 转换为 BMCLAPI
            if (mojangUrl.Contains("piston-data.mojang.com") || mojangUrl.Contains("launcher.mojang.com"))
            {
                // 提取路径部分
                var uri = new Uri(mojangUrl);
                var path = uri.AbsolutePath;
                return $"https://bmclapi2.bangbang93.com{path}";
            }
            return mojangUrl;
        }

        /// <summary>
        /// 将 Mojang 版本 JSON URL 转换为 BMCLAPI 镜像
        /// </summary>
        private string ConvertToBmclVersionUrl(string mojangUrl)
        {
            // 将 https://piston-meta.mojang.com/... 转换为 BMCLAPI
            if (mojangUrl.Contains("piston-meta.mojang.com") || mojangUrl.Contains("launchermeta.mojang.com"))
            {
                var uri = new Uri(mojangUrl);
                var path = uri.AbsolutePath;
                return $"https://bmclapi2.bangbang93.com{path}";
            }
            return mojangUrl;
        }

        /// <summary>
        /// 计算文件 SHA1
        /// </summary>
        private async Task<string> CalculateSha1Async(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = await sha1.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// 版本详情
    /// </summary>
    public class VersionDetail
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Time { get; set; } = "";
        public string ReleaseTime { get; set; } = "";
        public Downloads? Downloads { get; set; }
        public AssetIndex? AssetIndex { get; set; }
        public List<Library>? Libraries { get; set; }
        public Logging? Logging { get; set; }
        public string MainClass { get; set; } = "";
        public int MinimumLauncherVersion { get; set; }
    }

    public class Downloads
    {
        public DownloadInfo? Client { get; set; }
        public DownloadInfo? Client_mappings { get; set; }
        public DownloadInfo? Server { get; set; }
        public DownloadInfo? Server_mappings { get; set; }
    }

    public class DownloadInfo
    {
        public string Sha1 { get; set; } = "";
        public int Size { get; set; }
        public string Url { get; set; } = "";
    }

    public class AssetIndex
    {
        public string Id { get; set; } = "";
        public string Sha1 { get; set; } = "";
        public int Size { get; set; }
        public int TotalSize { get; set; }
        public string Url { get; set; } = "";
    }

    public class Library
    {
        public string Name { get; set; } = "";
        public Downloads? Downloads { get; set; }
        public List<Rule>? Rules { get; set; }
    }

    public class Rule
    {
        public string Action { get; set; } = "";
        public Os? Os { get; set; }
    }

    public class Os
    {
        public string Name { get; set; } = "";
    }

    public class Logging
    {
        public LoggingClient? Client { get; set; }
    }

    public class LoggingClient
    {
        public string Argument { get; set; } = "";
        public LoggingFile? File { get; set; }
        public string Type { get; set; } = "";
    }

    public class LoggingFile
    {
        public string Id { get; set; } = "";
        public string Sha1 { get; set; } = "";
        public int Size { get; set; }
        public string Url { get; set; } = "";
    }
}
