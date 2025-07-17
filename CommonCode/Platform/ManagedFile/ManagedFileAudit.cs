using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;

///////////// PUT SPACING BETWEEN PROPERTIES, ELEMENTS


// <summary>
/// A log of what happened to a file when it happened.
/// </summary>
public class ManagedFileAudit: IDataModel
{

    [BsonId]
    public Guid Id { get; set; }
    /// <summary>
    /// What version of the file this is useful for when multple users are modifying a file.
    /// 
    /// 
    /// NEED to be TECHNICAL in TERMINOLOGY- eg, 'version to support optimistic concurrency checking on updates'
    /// </summary>
    public int Version { get; set; }
 
    /// <summary>
    /// When this audit went through.
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// An event for passing the audit info on to the rules.
    /// </summary>
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public ManagedFileEvents Event { get; set; }
  
    /// <summary>
    /// The ID of the file. Required for IDataModel.
    /// </summary>
    public Guid ManagedFileId { get; set; }
 
    /// <summary>
    /// The original file name.
    /// </summary>
    public string OriginalFileName { get; set; } = "";
    /// <summary>
    /// What happened with the file why is it being audited.
    /// </summary>
    public string Information { get; set; } = "";
    /// <summary>
    /// Who/What audited the file.
    /// </summary>
    public Guid? Actor { get; set; }
}
