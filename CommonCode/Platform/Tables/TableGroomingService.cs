using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Hosting;

namespace BFormDomain.CommonCode.Platform.Tables;

/// <summary>
/// TableGroomingService grooms table data from the report instance data where the creation date is less than the expiration time
///     -References:
///         >Service
///     -Functions:
///         >ExecuteAsync
/// </summary>
public class TableGroomingService : BackgroundService
{

    private readonly IRepository<TableMetadata> _metadata;
    private readonly KeyInject<string, TableDataRepository>.ServiceResolver _repoFactory;
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationPlatformContent _content;


    public TableGroomingService(
        IApplicationAlert alerts,
        IRepository<TableMetadata> metadata,
        IApplicationPlatformContent content,
        KeyInject<string, TableDataRepository>.ServiceResolver repoFactory)
    {
        _alerts = alerts;
        _metadata= metadata;
        _repoFactory= repoFactory;
        _content= content;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (needsGrooming, rc) = await _metadata.GetAllAsync(md =>
                    md.NeedsGrooming &&
                    md.NextGrooming < DateTime.UtcNow);

                if(needsGrooming.Any())
                {
                 
                    Parallel.ForEach(needsGrooming, async md =>
                    {
                        try
                        {
                            var template = _content.GetContentByName<TableTemplate>(md.TemplateName)!;
                            template.Guarantees().IsNotNull();

                            var data = _repoFactory(TableDataRepository.Scope);
                            data.Initialize(template, md.PerWorkSet, md.PerWorkSet);

                            var expirationTime = DateTime.UtcNow
                                                    .AddMonths(-1 * md.MonthsRetained)
                                                    .AddDays(-1 * md.DaysRetained)
                                                    .AddHours(-1 * md.HoursRetained)
                                                    .AddMinutes(-1 * md.MinutesRetained);

                            var (toGroom, _) = await data.GetAllAsync(md => md.Created < expirationTime);

                            await data.DeleteBatchAsync(toGroom.Select(it => it.Id));

                            var nextGroomTime = DateTime.UtcNow
                                                .AddMonths(md.MonthsRetained)
                                                .AddDays(md.DaysRetained)
                                                .AddHours(md.HoursRetained)
                                                .AddMinutes(md.MinutesRetained);

                            md.LastGrooming = DateTime.UtcNow;
                            md.NextGrooming = nextGroomTime;
                            await _metadata.UpdateAsync(md);
                        }
                        catch (Exception x)
                        {
                            _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                                    x.TraceInformation(), 1);
                        }


                    });

                } 
                
                await Task.Delay(60 * 1000, stoppingToken);
                
            }
            catch (OperationCanceledException)
            {
                // catch the cancellation exception
                // to stop execution
                return;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, Microsoft.Extensions.Logging.LogLevel.Information,
                    ex.TraceInformation(), 1);
            }
        }
    }




}
