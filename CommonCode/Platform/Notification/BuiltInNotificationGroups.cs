using BFormDomain.HelperClasses;
using BFormDomain.Repository;

namespace BFormDomain.CommonCode.Notification;

/// <summary>
/// BuiltInNotificationGroups defines pre-determined notification groups
///     -References:
///         >SwitchingApplicationAlert.cs
///     -Functions:
///         >MaybeInitialize
///         >GetApplicationAlertsGroupAsync
/// </summary>
public class BuiltInNotificationGroups
{

    private readonly IRepository<NotificationGroup> _repo;
    private readonly NotificationGroup _alertsGroup;
    private readonly Guid _alertsGroupId;

    public BuiltInNotificationGroups(IRepository<NotificationGroup> repo)
    {
        _repo = repo;
        _alertsGroupId = new Guid("affb5044-8981-4661-bbe7-3478fd7a115d");

        _alertsGroup = new NotificationGroup
        {
            Id = _alertsGroupId,
            Active = true,
            GroupDescription = "Receives application alert notifications.",
            GroupTitle = "Application Alerts"
        };

        // TODO: Any others?

    }

    public async Task<NotificationGroup> GetApplicationAlertsGroupAsync()
    {
        var (retval, _) = await _repo.LoadAsync(_alertsGroupId);
        return retval;
    }

    public Guid ApplicationAlertsGroupId { get { return _alertsGroupId; } }

}
