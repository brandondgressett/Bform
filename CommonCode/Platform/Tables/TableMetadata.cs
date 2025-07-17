using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Platform.Tables;

public class TableMetadata : IDataModel
{
    [BsonId]
    public Guid Id { get; set; }

    public int Version { get; set; }

    public string TemplateName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public Guid? PerWorkSet { get; set; }
    public Guid? PerWorkItem { get; set; }

    public bool NeedsGrooming { get; set; }
    public DateTime LastGrooming { get; set; }
    public DateTime NextGrooming { get; set; }
    public int MonthsRetained { get; set; }
    public int DaysRetained { get; set; }
    public int HoursRetained { get; set; }
    public int MinutesRetained { get; set; }

    public DateTime Created { get; set; }

}
