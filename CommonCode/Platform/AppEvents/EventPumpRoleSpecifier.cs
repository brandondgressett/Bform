using BFormDomain.CommonCode.ApplicationTopology;

namespace BFormDomain.CommonCode.Platform.AppEvents;


/// <summary>
/// When registered with the DI container, declares a server role for
/// the ApplicationServerMonitor to track.
/// </summary>
public class EventPumpRoleSpecifier : IServerRoleSpecifier
{
    /// <summary>
    /// CAG RE
    /// </summary>
    public const string Name = nameof(AppEventPump);
    /// <summary>
    /// CAG RE
    /// </summary>
    public string RoleName { get { return Name; } }
    /// <summary>
    /// CAG RE
    /// </summary>
    public ServerRoleBalance RoleBalance { get { return ServerRoleBalance.Balance; } }
}
