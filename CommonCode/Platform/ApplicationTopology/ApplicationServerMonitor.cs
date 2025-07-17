using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using Microsoft.Extensions.Hosting;

namespace BFormDomain.CommonCode.ApplicationTopology;

/// <summary>
/// ApplicationServerMonitor 
///     -References:
///         >Background service
///     -Functions:
///         >MaybeInitialize
///         >SetMeUp
///         >ExecuteAsync
/// </summary>
public class ApplicationServerMonitor : BackgroundService
{
    private const string ApplicationServerMonitorRole = "application monitor";

    private readonly List<IServerRoleSpecifier> _serverRoles;
    private readonly IRepository<ApplicationServerRecord> _repo;
    private readonly string _serverName;
    private readonly IApplicationAlert _alerts;
    private readonly ApplicationTopologyCatalog _catalog;
    
    
    public ApplicationServerMonitor(
        IEnumerable<IServerRoleSpecifier> serverRoles,
        IRepository<ApplicationServerRecord> repo,
        ApplicationTopologyCatalog catalog,
        IApplicationAlert alerts)
        
    {
        _serverRoles = serverRoles.ToList();
        _repo = repo;
        _serverName = Environment.MachineName;
        _alerts = alerts;
        _catalog = catalog;
    }

    private void MaybeInitialize()
    {
        RunOnce.ThisCode(() =>
        {
            SetMeUp();
        });
    }

    /// <summary>
    /// SetMeUp makes sure current server actually exists
    /// </summary>
    private void SetMeUp()
    {
        AsyncHelper.RunSync(() => _repo.DeleteFilterAsync(it => it.ServerName == _serverName));
        _repo.Create(new ApplicationServerRecord
        {
            Id = Guid.NewGuid(),
            ServerName = _serverName,
            LastPingTime = DateTime.UtcNow
        });
    }

    /// <summary>
    /// ExecuteAsync updates server/servers by server role. 
    /// </summary>
    /// <param name="stoppingToken">Cancellation Token</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                MaybeInitialize();

                
                var (me, rc) = await _repo.GetOneAsync(it => it.ServerName == _serverName);
                if(me is null)
                {
                    SetMeUp();
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                me.LastPingTime = DateTime.UtcNow;
                await _repo.UpdateAsync((me, rc));
                _catalog.RefreshServerRoles(me.ServerRoles);

                var (runningServers, _) = await _repo.GetAllAsync(it => it.LastPingTime > DateTime.UtcNow.AddMinutes(-1.0));

                if (runningServers.Count <= 1)
                {
                    if (me.ServerName != _serverName)
                    {
                        SetMeUp();
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    me.ServerRoles.Clear();
                    me.ServerRoles.AddRange(_serverRoles.Select(it => it.RoleName));
                    me.ServerRoles.Add(ApplicationServerMonitorRole);
                    
                    me.LastPingTime = DateTime.UtcNow;

                    await _repo.UpdateAsync((me, rc));
                    await Task.Delay(1000, stoppingToken);
                } else if(runningServers.Any())
                {
                    var monitor = runningServers
                        .OrderBy(it=>it.ServerName)
                        .FirstOrDefault(server=>server.
                                    ServerRoles.Contains(ApplicationServerMonitorRole));

                    if(monitor is not null && monitor.ServerName != _serverName)
                    {
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    } else if(monitor is null)
                    {
                        me.ServerRoles.Add(ApplicationServerMonitorRole);
                        await _repo.UpdateIgnoreVersionAsync((me, rc));
                        
                        continue;
                    }

                    foreach(var role in _serverRoles)
                    {
                        if (runningServers.Any(server => server.ServerRoles.Contains(role.RoleName)))
                            continue;

                        var maxLoaded = runningServers.MaxBy(it => it.ServerRoles.Count);
                        var minLoaded = runningServers.MinBy(it => it.ServerRoles.Count);

                        switch(role.RoleBalance)
                        {
                            case ServerRoleBalance.StackOnWorkhorse:
                                maxLoaded!.ServerRoles.Add(role.RoleName);
                                await _repo.UpdateAsync((maxLoaded, rc));
                                break;

                            case ServerRoleBalance.Balance:
                                minLoaded!.ServerRoles.Add(role.RoleName);
                                await _repo.UpdateAsync((minLoaded, rc));
                                break;
                        }
                    }
                }
                
            }
            catch (OperationCanceledException)
            {
                // catch the cancellation exception
                // to stop execution
                return;
            }
            catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                    ex.TraceInformation(), 4);
            }
        }
    }
}

