using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.ManagedFiles;

namespace BFormDomain.CommonCode.Platform.Forms;

public class FormInstanceViewModel
{
    public Guid Id { get; set; }
    public FormTemplateViewModel? Template { get; set; }
    public string? ActionCompletionId { get; set; } = "";
    public string? JsonContent { get; set; } = "";

    public Guid? WorkSet { get; set; }
    public Guid? WorkItem { get; set; }
    public Guid? Creator { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    
    public FormInstanceHome Home {get; set;}

    public List<string> Tags { get; set; } = new();
    public List<CommentViewModel> Comments { get; set; } = new();
    public List<ManagedFileViewModel> Files { get; set; } = new();
}
