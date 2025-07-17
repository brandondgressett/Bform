namespace BFormDomain.Diagnostics;

public interface IPerformanceMetricPersistence
{
    void Record(DateTime start, DateTime end, string name, int count, double rpm, double rps, double maxMS, double minMS, double medMS, double avgMS, double sumMS, string file, int ln, string machine);
}
