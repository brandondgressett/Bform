using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Tables;
using BFormDomain.HelperClasses;
using HTMLReportEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Reports;

/// <summary>
/// ReportTemplate builds reports based on the properties of the template
///     -References:
///         >FileApplicationPlatformContent.cs
///         >IApplicationPlatformContent.cs
///     -Functions:
///         >BuildReport
///         >BuildTitleDetails
///         >GetTitleElement
/// </summary>
public class ReportTemplate : IContentType
{
    public string Name { get; set; } = null!;
    public int DescendingOrder { get; set; }
    public string? DomainName { get; set; } = nameof(ReportTemplate);

    public Dictionary<string, string>? SatelliteData { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public string Description { get; set; } = "";

    public RelativeTableQueryCommand Query { get; set; } = null!;
    public string? QueryFormTemplate { get; set; }
    public bool GroomReportInstances { get; set; }
    public int GroomingLifeDays { get; set; }
    public string TitleTemplate { get; set; } = null!;
    public string ReportFont { get; set; } = "Arial";
    public List<string> TotalFields { get; set; } = new();
    public bool IncludeTotal { get; set; }
    public List<Section> Sections { get; set; } = new();
    public List<Field> Fields { get; set; } = new();

    public List<ColDef> TitleElements { get; set; } = new();
    
    public ChartSpec? ChartSpec { get; set; }
    public List<string> NotificationGroupsWithTags { get; set; } = new();
    public bool SMSNotify { get; set; }
    public bool EmailNotify { get; set; }
    public bool ToastNotify { get; set; }
    public bool CallNotify { get; set; }

    public int? SuppressionMinutes { get; set; }

    
    public string? IconClass { get; set; }

    public bool UserCreatable { get; set; } = true;



    private string GetTitleElement(JObject queryForm, ColDef cd, TimeZoneInfo tzi)
    {
        
        string valStr = string.Empty;
        switch(cd.Type)
        {
            case JTokenType.Integer:
                var intData = (int) queryForm.SelectToken(cd.Field)!;
                valStr = intData.ToString();
                break;
            case JTokenType.Float:
                var fltData = (float)queryForm.SelectToken(cd.Field)!;
                valStr = fltData.ToString("0.00");
                break;
            case JTokenType.String:
                valStr = (string)queryForm.SelectToken(cd.Field)!;
                break;
            case JTokenType.Boolean:
                valStr = ((bool)queryForm.SelectToken(cd.Field)!).ToString();
                break;
            case JTokenType.Date:
                var dtData = (DateTime)queryForm.SelectToken(cd.Field)!;
                var local = TimeZoneInfo.ConvertTimeFromUtc(dtData, tzi);
                valStr = $"{local.ToShortDateString()} {local.ToShortTimeString()}";
                break;

        }

        return $"{cd.HeaderName}: {valStr}";
    }

    private string BuildTitleDetails(JObject queryForm, string tzId)
    {
        var localTz = TimeZoneInfo.FromSerializedString(tzId);
        return string.Join(",",
            TitleElements.Select(elem => GetTitleElement(queryForm, elem, localTz)));
    }

    public Report BuildReport(DataSet data, JObject queryForm, string tzId)
    {
        var report = new Report();

        if(!string.IsNullOrWhiteSpace(ReportFont))
            report.ReportFont= ReportFont;

        report.IncludeTotal = IncludeTotal;

        report.TotalFields = new ArrayList();
        report.TotalFields.AddRange(TotalFields);
       
        var reportSections = new ArrayList();
        reportSections.AddRange(Sections);
        report.Sections = reportSections;

        var reportFields = new ArrayList();
        reportFields.AddRange(Fields);
        report.ReportFields = reportFields;


        if(ChartSpec is not null)
        {
            report.IncludeChart = true;
            report.ChartTitle = ChartSpec.ChartTitle;
            report.ChartShowAtBottom = ChartSpec.ChartShowAtBottom;
            if(ChartSpec.ChartChangeOnField is not null)
                report.ChartChangeOnField = ChartSpec.ChartChangeOnField;
            report.ChartShowBorder = ChartSpec.ChartShowBorder;
            report.ChartLabelHeader = ChartSpec.ChartLabelHeader;
            report.ChartPercentageHeader = ChartSpec.ChartPercentageHeader;
            report.ChartValueHeader = ChartSpec.ChartValueHeader;
        } else
            report.IncludeChart = false;


        string reportTitle = TitleTemplate;
        if(TitleElements.EmptyIfNull().Any())
        {
            var details = BuildTitleDetails(queryForm, tzId);
            reportTitle = $"{reportTitle} - {details}";
        }

        report.ReportTitle = reportTitle;

        report.ReportSource = data;
        return report;
    }
}
