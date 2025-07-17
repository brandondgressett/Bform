using AspNetCore.Identity.MongoDbCore.Models;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.DataModels;
using MongoDbGenericRepository.Attributes;

namespace BFormDomain.CommonCode.Authorization;

[CollectionName("ApplicationRoles")]
public class ApplicationRole: MongoIdentityRole<Guid>, IDataModel, ITenantScoped
{
    public new int Version { get; set; }
    
    /// <summary>
    /// Description of the role
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// List of claims associated with this role
    /// </summary>
    public new List<string> Claims { get; set; } = new();
    
    /// <summary>
    /// The tenant this role belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    public ApplicationRole(): base()
    {

    }

    public ApplicationRole(string roleName): base(roleName)
    {

    }

}
