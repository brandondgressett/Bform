namespace BFormDomain.CommonCode.Authorization;

public class ApplicationUserViewModel
{
    public string UserName { get; set; } = "";
    public List<string> RoleNames { get; set; } = new();
    public string? TimeZoneId { get; set; } = "";
    public string Email { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
}
