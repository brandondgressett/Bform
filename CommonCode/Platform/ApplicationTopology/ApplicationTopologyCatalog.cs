using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.ApplicationTopology;

public class ApplicationTopologyCatalogOptions
{
    public bool SingleServerMode { get; set; } = false;
}

/// <summary>
/// Register as singleton.
/// </summary>
public class ApplicationTopologyCatalog
{
    private readonly object _lock = new();
    private readonly List<string> _serverRoles = new();
    private readonly bool _singleServerMode = false;

    public ApplicationTopologyCatalog(IOptions<ApplicationTopologyCatalogOptions> options)
    {
        _singleServerMode = options.Value.SingleServerMode;
    }

    public bool IsThisServerInRole(string serverRoleName)
    {
        if(_singleServerMode)
            return true;

        lock(_lock)
        {
            return _serverRoles.Contains(serverRoleName);
        }
    }


    public void RefreshServerRoles(IEnumerable<string> serverRoles)
    {
        lock(_lock)
        {
            _serverRoles.Clear();
            _serverRoles.AddRange(serverRoles);
        }
    }





}
