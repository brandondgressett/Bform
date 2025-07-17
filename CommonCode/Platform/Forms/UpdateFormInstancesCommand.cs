namespace BFormDomain.CommonCode.Platform.Forms;

public class UpdateFormInstancesCommand
{
    public Guid Id { get; set; }
    public string ContentJson { get; set; } = "";
    public IEnumerable<string>? Tags { get; set; }

}
