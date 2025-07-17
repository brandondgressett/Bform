using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems;

public class CreateWorkItemCommand
{
    public string? TemplateName { get; set; }
    public string? TemplateNameQuery { get; set; }

    public string? WorkSetQuery { get; set; }
    public List<string>? WorkSetTagged { get; set; }

    public string? Title { get; set; }
    public string? TitleQuery { get; set; }
    public string? Description { get; set; }
    public string? DescriptionQuery { get; set; }

    public bool? IsListed { get; set; }
    public string? IsListedQuery { get; set; }
    public bool? IsVisible { get; set; }
    public string? IsVisibleQuery { get; set; }


    public string? UserAssigneeQuery { get; set; }
    public string? TriageAssigneeQuery { get; set; }
    public int TriageAssignee { get; set; }

    public int? Status { get; set; }
    public string? StatusQuery { get; set; }

    public int? Priority { get; set; }
    public string? PriorityQuery { get; set; }

    public JObject? CreationData { get; set; }
    public string? CreationDataQuery { get; set; }


    public List<string> InitialTags { get; set; } = new();
}
