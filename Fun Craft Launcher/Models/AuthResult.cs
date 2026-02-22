namespace FunCraftLauncher.Models
{
    public enum LoginType
    {
        Microsoft,
        Offline
    }

    public class AuthResult
    {
        public LoginType LoginType { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UUID { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public bool IsPremium => LoginType == LoginType.Microsoft;
    }
}
