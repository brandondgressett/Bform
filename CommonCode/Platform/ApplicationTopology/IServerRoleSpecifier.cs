namespace BFormDomain.CommonCode.ApplicationTopology;

/// <summary>
/// Register implementations of this interface 
/// for each server role.
/// </summary>
public interface IServerRoleSpecifier
{
    string RoleName { get; }
    
    ServerRoleBalance RoleBalance { get;  }

}
