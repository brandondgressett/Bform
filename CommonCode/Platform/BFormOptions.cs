namespace BFormDomain.CommonCode.Platform;

public class BFormOptions
{
    public string DefaultTimeZoneId { get; set; } = "";
    public string DomainBaseUrl { get; set; } = "localhost://";

    public string ReportViewerUrl { get; set; } = "viewreport/{id}";

    public string GetLocalTimeZoneId()
    {
        string id = DefaultTimeZoneId;
        if(string.IsNullOrEmpty(id))
            id = TimeZoneInfo.Local.ToSerializedString();
        return id;
    }
}
