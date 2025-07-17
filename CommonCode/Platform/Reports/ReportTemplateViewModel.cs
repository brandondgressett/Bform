using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Reports;

public class ReportTemplateViewModel
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<string> Tags { get; set; } = new List<string>();   
    public string? IconClass { get; set; }

    public bool UserCreatable { get; set; }

    public static ReportTemplateViewModel Create(ReportTemplate template)
    {
        return new ReportTemplateViewModel
        {
            Name = template.Name,
            Description = template.Description,
            IconClass = template.IconClass,
            Tags = template.Tags,
            UserCreatable = template.UserCreatable,

        };
    }


}
