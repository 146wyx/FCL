using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Services
{
    public class ModLoaderService
    {
        private readonly HttpClient _httpClient;

        public ModLoaderService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 获取 Forge 版本列表
        /// </summary>
        public async Task<List<ForgeVersion>> GetForgeVersionsAsync(string mcVersion)
        {
            try
            {
                var url = $"https://bmclapi2.bangbang93.com/forge/minecraft/{mcVersion}";
                LogService.Instance.WriteInfo("ModLoaderService", $"获取 Forge 版本列表: {mcVersion}");
                
                var response = await _httpClient.GetStringAsync(url);
                var versions = JsonSerializer.Deserialize<List<ForgeVersion>>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return versions ?? new List<ForgeVersion>();
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("ModLoaderService", $"获取 Forge 版本列表失败: {mcVersion}", ex);
                return new List<ForgeVersion>();
            }
        }

        /// <summary>
        /// 获取 NeoForge 版本列表
        /// </summary>
        public async Task<List<NeoForgeVersion>> GetNeoForgeVersionsAsync(string mcVersion)
        {
            try
            {
                // NeoForge 使用 Maven API 获取版本
                var url = $"https://bmclapi2.bangbang93.com/neoforge/list/{mcVersion}";
                LogService.Instance.WriteInfo("ModLoaderService", $"获取 NeoForge 版本列表: {mcVersion}");
                
                var response = await _httpClient.GetStringAsync(url);
                var versions = JsonSerializer.Deserialize<List<NeoForgeVersion>>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return versions ?? new List<NeoForgeVersion>();
            }
            catch (Exception ex)
            {
                LogService.Instance.WriteError("ModLoaderService", $"获取 NeoForge 版本列表失败: {mcVersion}", ex);
                return new List<NeoForgeVersion>();
            }
        }

        /// <summary>
        /// 构建 Forge 下载链接
        /// </summary>
        public string BuildForgeDownloadUrl(string mcVersion, string forgeVersion, string category = "installer", string format = "jar")
        {
            // https://bmclapi2.bangbang93.com/forge/download?mcversion={mcversion}&version={version}&category={category}&format={format}
            return $"https://bmclapi2.bangbang93.com/forge/download?mcversion={mcVersion}&version={forgeVersion}&category={category}&format={format}";
        }

        /// <summary>
        /// 构建 NeoForge 下载链接
        /// </summary>
        public string BuildNeoForgeDownloadUrl(string mcVersion, string neoForgeVersion)
        {
            // https://bmclapi2.bangbang93.com/neoforge/download?mcversion={mcversion}&version={version}
            return $"https://bmclapi2.bangbang93.com/neoforge/download?mcversion={mcVersion}&version={neoForgeVersion}";
        }
    }

    public class ForgeVersion
    {
        public string Branch { get; set; } = "";
        public int Build { get; set; }
        public string Mcversion { get; set; } = "";
        public string Modified { get; set; } = "";
        public string Version { get; set; } = "";
        public string _id { get; set; } = "";
        public List<ForgeFile> Files { get; set; } = new();
    }

    public class ForgeFile
    {
        public string Format { get; set; } = "";
        public string Category { get; set; } = "";
        public string Hash { get; set; } = "";
        public string _id { get; set; } = "";
    }

    public class NeoForgeVersion
    {
        public string Version { get; set; } = "";
        public string McVersion { get; set; } = "";
        public string Modified { get; set; } = "";
    }
}
