namespace BFormDomain.CommonCode.Authorization;

public class TokenRequest
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}
