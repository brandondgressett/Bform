namespace BFormDomain.CommonCode.Platform.WorkSets;

public enum WorkSetManagement
{
    /// <summary>
    /// Users can create, delete, lock worksets
    /// </summary>
    UserManaged,

    /// <summary>
    /// creation, deletion, lock of workset done all by rules.
    /// </summary>
    RulesManaged
}
