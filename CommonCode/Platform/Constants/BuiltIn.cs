namespace BFormDomain.CommonCode.Platform.Constants;

public static class BuiltIn
{
    public static readonly Guid SystemUser = new("640ea5c6-c551-4589-90dd-af98b6d01650");
    public static readonly Guid SystemWorkSet = new("6f9c6a4e-6308-4c04-ae00-23fadeba83f4");
    public static readonly Guid SystemWorkItem = new("fe2d8728-db7b-455e-8b2f-28a215f4416a");

    public const string BFormHtml = "<!-- BForm HTML -->";

    public const string UserReaderName = "DemoAdmin";
    public const string UserReaderEmail = "demoadmin@bform.com";
    public const string UserReaderPassword = "demoadmin";

    public const string SiteReaderName = "sitereader";
    public const string SiteReaderEmail = "sitereader@bform.com";
    public const string SiteReaderPassword = "sitereader";
}
