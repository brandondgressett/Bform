namespace BFormDomain.CommonCode.Authorization;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public List<string> Roles { get; set; } = new List<string>();
}
