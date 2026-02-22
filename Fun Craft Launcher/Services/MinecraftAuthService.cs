using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FunCraftLauncher.Models;
using FunCraftLauncher.Services;

namespace FunCraftLauncher.Services
{
    public class MinecraftAuthService
    {
        private readonly HttpClient _httpClient;

        public MinecraftAuthService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 完整的 Xbox Live -> XSTS -> Minecraft 认证流程
        /// </summary>
        public async Task<AuthResult?> AuthenticateAsync(string microsoftAccessToken)
        {
            try
            {
                // 1. 获取 Xbox Live Token
                var xboxToken = await GetXboxLiveTokenAsync(microsoftAccessToken);
                if (xboxToken == null)
                {
                    throw new Exception("Xbox Live Token 获取失败");
                }

                // 2. 获取 XSTS Token
                var xstsToken = await GetXSTSTokenAsync(xboxToken.Token, xboxToken.UserHash);
                if (xstsToken == null)
                {
                    throw new Exception("XSTS Token 获取失败");
                }

                // 3. 获取 Minecraft Access Token
                var minecraftToken = await GetMinecraftTokenAsync(xstsToken.Token, xstsToken.UserHash);
                if (minecraftToken == null)
                {
                    throw new Exception("Minecraft Access Token 获取失败");
                }

                // 4. 检查是否拥有 Minecraft
                var ownsMinecraft = await CheckGameOwnershipAsync(minecraftToken.AccessToken);
                if (!ownsMinecraft)
                {
                    throw new Exception("此账户未购买 Minecraft Java Edition");
                }

                // 5. 获取玩家档案信息
                var profile = await GetMinecraftProfileAsync(minecraftToken.AccessToken);
                if (profile == null)
                {
                    throw new Exception("Minecraft 玩家档案获取失败");
                }

                return new AuthResult
                {
                    LoginType = LoginType.Microsoft,
                    Username = profile.Name,
                    UUID = profile.Id,
                    AccessToken = minecraftToken.AccessToken,
                    IsSuccessful = true
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Minecraft 认证失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 1. 获取 Xbox Live Token
        /// </summary>
        private async Task<XboxTokenResponse?> GetXboxLiveTokenAsync(string microsoftAccessToken)
        {
            var url = "https://user.auth.xboxlive.com/user/authenticate";

            // 构建请求体，使用正确的格式
            var requestBody = new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = "d=" + microsoftAccessToken
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 创建请求消息
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // 添加请求头
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            LogService.Instance.WriteInfo("MinecraftAuthService", "正在发送Xbox Live Token请求");
            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            LogService.Instance.WriteInfo("MinecraftAuthService", $"Xbox Live Token响应状态码: {response.StatusCode}");
            LogService.Instance.WriteInfo("MinecraftAuthService", $"Xbox Live Token响应内容: {responseString}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Xbox Live Token 获取失败: {response.StatusCode}\n{responseString}");
            }

            var xboxResponse = JsonSerializer.Deserialize<XboxAuthResponse>(responseString);
            LogService.Instance.WriteInfo("MinecraftAuthService", $"Xbox Live Token解析结果: Token={xboxResponse?.Token != null}, DisplayClaims={xboxResponse?.DisplayClaims != null}, Xui长度={xboxResponse?.DisplayClaims?.Xui?.Length ?? 0}");

            if (xboxResponse?.Token == null || xboxResponse.DisplayClaims?.Xui == null || xboxResponse.DisplayClaims.Xui.Length == 0)
            {
                LogService.Instance.WriteError("MinecraftAuthService", "Xbox Live Token响应格式不正确");
                return null;
            }

            LogService.Instance.WriteInfo("MinecraftAuthService", "Xbox Live Token获取成功");
            return new XboxTokenResponse
            {
                Token = xboxResponse.Token,
                UserHash = xboxResponse.DisplayClaims.Xui[0].Uhs
            };
        }

        /// <summary>
        /// 2. 获取 XSTS Token
        /// </summary>
        private async Task<XSTSTokenResponse?> GetXSTSTokenAsync(string xboxToken, string userHash)
        {
            var url = "https://xsts.auth.xboxlive.com/xsts/authorize";

            var requestBody = new Dictionary<string, object>
            {
                ["Properties"] = new Dictionary<string, object>
                {
                    ["SandboxId"] = "RETAIL",
                    ["UserTokens"] = new[] { xboxToken }
                },
                ["RelyingParty"] = "rp://api.minecraftservices.com/",
                ["TokenType"] = "JWT"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<XSTSErrorResponse>(responseString);
                if (error?.XErr == 2148916233)
                    throw new Exception("此账户未关联 Xbox 账户");
                if (error?.XErr == 2148916238)
                    throw new Exception("此账户是未成年人，需要添加到家庭组");

                throw new Exception($"XSTS 认证失败: {responseString}");
            }

            var xstsResponse = JsonSerializer.Deserialize<XboxAuthResponse>(responseString);
            if (xstsResponse?.Token == null || xstsResponse.DisplayClaims?.Xui == null || xstsResponse.DisplayClaims.Xui.Length == 0)
            {
                return null;
            }

            return new XSTSTokenResponse
            {
                Token = xstsResponse.Token,
                UserHash = xstsResponse.DisplayClaims.Xui[0].Uhs
            };
        }

        /// <summary>
        /// 3. 获取 Minecraft Access Token
        /// </summary>
        private async Task<MinecraftTokenResponse?> GetMinecraftTokenAsync(string xstsToken, string userHash)
        {
            var url = "https://api.minecraftservices.com/authentication/login_with_xbox";

            var requestBody = new Dictionary<string, string>
            {
                ["identityToken"] = $"XBL3.0 x={userHash};{xstsToken}"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            LogService.Instance.WriteInfo("MinecraftAuthService", "正在发送Minecraft登录请求");
            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            LogService.Instance.WriteInfo("MinecraftAuthService", $"Minecraft登录响应状态码: {response.StatusCode}");
            LogService.Instance.WriteInfo("MinecraftAuthService", $"Minecraft登录响应内容: {responseString}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Minecraft 登录失败: {responseString}");
            }

            return JsonSerializer.Deserialize<MinecraftTokenResponse>(responseString);
        }

        /// <summary>
        /// 4. 检查是否拥有 Minecraft
        /// </summary>
        private async Task<bool> CheckGameOwnershipAsync(string minecraftAccessToken)
        {
            var url = "https://api.minecraftservices.com/entitlements/mcstore";

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", minecraftAccessToken);

            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var entitlements = JsonSerializer.Deserialize<EntitlementsResponse>(responseString);
            return entitlements?.Items != null && entitlements.Items.Length > 0;
        }

        /// <summary>
        /// 5. 获取 Minecraft 玩家档案
        /// </summary>
        private async Task<MinecraftProfile?> GetMinecraftProfileAsync(string minecraftAccessToken)
        {
            var url = "https://api.minecraftservices.com/minecraft/profile";

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", minecraftAccessToken);

            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"获取 Minecraft 档案失败: {responseString}");
            }

            return JsonSerializer.Deserialize<MinecraftProfile>(responseString);
        }
    }

    // 数据模型
    public class XboxTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserHash { get; set; } = string.Empty;
    }

    public class XSTSTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserHash { get; set; } = string.Empty;
    }

    public class MinecraftTokenResponse
    {
        public string Username { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public long ExpiresIn { get; set; }
    }

    public class MinecraftProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Skin[] Skins { get; set; } = Array.Empty<Skin>();
        public Cape[] Capes { get; set; } = Array.Empty<Cape>();
    }

    public class Skin
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Variant { get; set; } = string.Empty;
    }

    public class Cape
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class XboxAuthResponse
    {
        public string IssueInstant { get; set; } = string.Empty;
        public string NotAfter { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DisplayClaims DisplayClaims { get; set; } = new DisplayClaims();
    }

    public class DisplayClaims
    {
        [System.Text.Json.Serialization.JsonPropertyName("xui")]
        public Xui[] Xui { get; set; } = Array.Empty<Xui>();
    }

    public class Xui
    {
        [System.Text.Json.Serialization.JsonPropertyName("uhs")]
        public string Uhs { get; set; } = string.Empty;
    }

    public class XSTSErrorResponse
    {
        public long XErr { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Redirect { get; set; } = string.Empty;
    }

    public class EntitlementsResponse
    {
        public Item[] Items { get; set; } = Array.Empty<Item>();
        public string Signature { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
    }

    public class Item
    {
        public string Name { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}
