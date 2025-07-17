namespace BFormDomain.Diagnostics;

public class FilePerformanceMetricPersistenceOptions
{
    public string PerformanceReportPath { get; set; } = "Performance";
    public int RetentionDays { get; set; } = 7;
    public int FaultTolerance { get; set; } = 15;
}
