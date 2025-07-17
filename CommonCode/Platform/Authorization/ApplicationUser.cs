using AspNetCore.Identity.MongoDbCore.Models;
using BFormDomain.DataModels;
using MongoDbGenericRepository.Attributes;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Tenancy;

namespace BFormDomain.CommonCode.Authorization;

[CollectionName("ApplicationUsers")]
public class ApplicationUser: MongoIdentityUser<Guid>, IDataModel, ITenantScoped
{
    public string? TimeZoneId { get; set; }
    public List<string> Tags { get; set; } = new();
    public new int Version { get; set; }
    
    // Additional properties often referenced
    public string? DisplayName { get; set; }
    public string? Username => UserName;  // Alias for UserName
    public bool IsActive { get; set; } = true;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public new DateTime? CreatedOn { get; set; }
    public DateTime? DeactivatedOn { get; set; }

    // Multi-tenancy properties
    public Guid TenantId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public List<Guid> AccessibleTenantIds { get; set; } = new();

    public ApplicationUser(): base()
    {

    }

    public ApplicationUser(string userName, string email, string tzid): base(userName, email)
    {
        TimeZoneId = tzid;
    }



}
