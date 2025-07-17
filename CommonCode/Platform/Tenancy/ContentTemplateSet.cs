using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbGenericRepository.Attributes;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Represents a set of content templates that can be used to initialize a new tenant.
/// Content template sets define the initial forms, rules, workflows, and other
/// content that a tenant will have when created.
/// </summary>
[CollectionName("ContentTemplateSets")]
public class ContentTemplateSet : IDataModel
{
    /// <summary>
    /// Unique identifier for the template set
    /// </summary>
    [BsonId]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Version for optimistic concurrency control
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Unique name for the template set (e.g., "basic", "enterprise", "healthcare")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this template set includes
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this template set is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether this is the default template set for new tenants
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// Tags for categorizing template sets
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Path to the content folder containing the templates
    /// Relative to the application content root
    /// </summary>
    public string ContentPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Manifest of content types included in this template set
    /// </summary>
    public ContentManifest Manifest { get; set; } = new();
    
    /// <summary>
    /// When this template set was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this template set was last updated
    /// </summary>
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Describes what content is included in a template set
/// </summary>
public class ContentManifest
{
    /// <summary>
    /// Number of form templates included
    /// </summary>
    public int FormTemplates { get; set; }
    
    /// <summary>
    /// Number of work item templates included
    /// </summary>
    public int WorkItemTemplates { get; set; }
    
    /// <summary>
    /// Number of work set templates included
    /// </summary>
    public int WorkSetTemplates { get; set; }
    
    /// <summary>
    /// Number of rules included
    /// </summary>
    public int Rules { get; set; }
    
    /// <summary>
    /// Number of scheduled event templates included
    /// </summary>
    public int ScheduledEventTemplates { get; set; }
    
    /// <summary>
    /// Number of report templates included
    /// </summary>
    public int ReportTemplates { get; set; }
    
    /// <summary>
    /// Number of KPI templates included
    /// </summary>
    public int KpiTemplates { get; set; }
    
    /// <summary>
    /// Number of table templates included
    /// </summary>
    public int TableTemplates { get; set; }
    
    /// <summary>
    /// Additional content types and their counts
    /// </summary>
    public Dictionary<string, int> AdditionalContent { get; set; } = new();
    
    /// <summary>
    /// List of specific features enabled by this template set
    /// </summary>
    public List<string> EnabledFeatures { get; set; } = new();
}