namespace BFormDomain.CommonCode.Platform.Rules;

public class RuleAction
{
    public string? Comment { get; set; }
    public List<RuleExpressionInvocation>? AppendBefore { get; set; }
    public RuleActionInvocation Invoke { get; set; } = null!;
    
    public List<RuleExpressionInvocation>? AppendAfter { get; set; }   

}
