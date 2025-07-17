namespace BFormDomain.Diagnostics;

public class SwitchingApplicationAlertOptions
{
    public bool Call { get; set; } = false;
    public bool Text { get; set; } = true;
    public bool Email { get; set; } = true;
}
