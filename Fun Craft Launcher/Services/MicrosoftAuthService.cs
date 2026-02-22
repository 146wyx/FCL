using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FunCraftLauncher.Services
{
    public class MicrosoftAuthService
    {
        private readonly HttpClient _httpClient;
        private const string DeviceCodeUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
        private const string TokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        private const string ClientId = "f1812aae-969e-48a0-80c4-8afbeb9703f7";

        public MicrosoftAuthService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 获取设备代码
        /// </summary>
        public async Task<DeviceCodeResult> GetDeviceCodeAsync()
        {
            var requestBody = new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"] = "XboxLive.signin offline_access"
            };

            var content = new FormUrlEncodedContent(requestBody);
            var response = await _httpClient.PostAsync(DeviceCodeUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"获取设备代码失败: {responseString}");
            }

            var result = JsonSerializer.Deserialize<DeviceCodeResponse>(responseString);
            if (result == null)
            {
                throw new Exception("解析设备代码响应失败");
            }

            return new DeviceCodeResult
            {
                DeviceCode = result.device_code,
                UserCode = result.user_code,
                VerificationUrl = result.verification_uri,
                ExpiresIn = result.expires_in,
                Interval = result.interval
            };
        }

        /// <summary>
        /// 使用刷新令牌获取新的访问令牌
        /// </summary>
        public async Task<MicrosoftTokenResult> RefreshTokenAsync(string refreshToken)
        {
            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = ClientId,
                ["refresh_token"] = refreshToken
            };

            var content = new FormUrlEncodedContent(requestBody);
            var response = await _httpClient.PostAsync(TokenUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"刷新令牌失败: {responseString}");
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseString);
            if (tokenResponse?.access_token == null)
            {
                throw new Exception("解析令牌响应失败");
            }

            return new MicrosoftTokenResult
            {
                AccessToken = tokenResponse.access_token,
                RefreshToken = tokenResponse.refresh_token,
                ExpiresIn = tokenResponse.expires_in,
                TokenType = tokenResponse.token_type
            };
        }

        /// <summary>
        /// 轮询获取 token
        /// </summary>
        public async Task<MicrosoftTokenResult> GetTokenAsync(string deviceCode, int interval, int expiresIn, Action<string>? onStatusUpdate = null)
        {
            var startTime = DateTime.UtcNow;
            var maxDuration = TimeSpan.FromSeconds(expiresIn);

            while (DateTime.UtcNow - startTime < maxDuration)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval));

                var requestBody = new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    ["client_id"] = ClientId,
                    ["device_code"] = deviceCode
                };

                var content = new FormUrlEncodedContent(requestBody);
                var response = await _httpClient.PostAsync(TokenUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseString);
                    if (tokenResponse?.access_token != null)
                    {
                        return new MicrosoftTokenResult
                        {
                            AccessToken = tokenResponse.access_token,
                            RefreshToken = tokenResponse.refresh_token,
                            ExpiresIn = tokenResponse.expires_in,
                            TokenType = tokenResponse.token_type
                        };
                    }
                }

                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseString);
                if (errorResponse?.error == "authorization_pending")
                {
                    onStatusUpdate?.Invoke("等待用户授权...");
                    continue;
                }
                else if (errorResponse?.error == "authorization_declined")
                {
                    throw new Exception("用户拒绝了授权");
                }
                else if (errorResponse?.error == "expired_token")
                {
                    throw new Exception("设备代码已过期");
                }
                else if (errorResponse?.error != null)
                {
                    throw new Exception($"获取 Token 失败: {errorResponse.error_description}");
                }
            }

            throw new Exception("设备代码已过期");
        }
    }

    public class DeviceCodeResult
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string VerificationUrl { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public int Interval { get; set; }
    }

    public class DeviceCodeResponse
    {
        public string device_code { get; set; } = string.Empty;
        public string user_code { get; set; } = string.Empty;
        public string verification_uri { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public int interval { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string scope { get; set; } = string.Empty;
        public string refresh_token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string error { get; set; } = string.Empty;
        public string error_description { get; set; } = string.Empty;
    }

    public class MicrosoftTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }
}
