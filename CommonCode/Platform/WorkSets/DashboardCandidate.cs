using BFormDomain.DataModels;

namespace BFormDomain.CommonCode.Platform.WorkSets;


public class DashboardCandidate : IDataModel
{
    public Guid Id { get; set; }

    public int Version { get; set; }

    public Guid WorkSet { get; set; }

    

    public int Score { get; set; }

    public int DescendingOrder { get; set; }



    public string? Grouping { get; set; }


    public DateTime Created { get; set; }


    public bool IsWinner { get; set; }


    public string EntityRef { get; set; } = null!;


    public string EntityType { get; set; } = null!;


    public string? TemplateName { get; set; } = null!;


    public List<string> Tags { get; set; } = new();

    public List<string> MetaTags { get; set; } = new();

}
