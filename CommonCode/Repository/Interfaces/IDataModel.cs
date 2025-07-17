namespace BFormDomain.DataModels;

public interface IDataModel
{
    Guid Id { get; set; }
    int Version { get; set; }

}
