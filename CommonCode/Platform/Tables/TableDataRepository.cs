using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables;

/// <summary>
/// Must be a transient instance -- Use KeyInject 
/// </summary>
public class TableDataRepository : MongoRepository<TableRowData>
{
    public const string Scope = nameof(TableRowData);

    private readonly IRepository<TableMetadata> _metadata;
    private readonly BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic _scheduler;
    

    public TableDataRepository(
        IRepository<TableMetadata> metadata,
        BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic scheduler,
        IOptions<MongoRepositoryOptions> options,
        SimpleApplicationAlert alerts) : base(options, alerts)
    {
        _metadata = metadata;
        _scheduler = scheduler;
    }

    private string _name = "";
    private bool _initialized = false;
    private const string DuplicateCollectionException = "E11000 duplicate key error collection";

    protected override string CollectionName 
    {
        get 
        {
            _name.Requires().IsNotNullOrEmpty();
            return _name; 
        }
    }

    public void Initialize(TableTemplate template, Guid? workSet, Guid? workItem)
    {
        var sb = new StringBuilder();
        sb.Append($"{template.CollectionName}");

        if(template.IsPerWorkItem)
        {
            string? id = "none";
            if (workItem is not null)
                id = GuidEncoder.Encode(workItem.Value);
            sb.Append($"_wi{id}");
        }
        else if (template.IsPerWorkSet)
        {
            string? id = "none";
            if (workSet is not null)
                id = GuidEncoder.Encode(workSet.Value);
            sb.Append($"_ws{id}");
        }

        _name = $"bftd_{sb}";

        _name.Guarantees().IsShorterThan(120); // mongo collection name len

        lock (_metadata)
        {
            var (matches, _) = _metadata.GetAll(md => md.CollectionName == _name);
            if (!matches.Any())
            {
                var instance = new TableMetadata
                {
                    CollectionName = _name,
                    Id = template.CollectionId ?? Guid.NewGuid(),
                    TemplateName = template.Name,
                    PerWorkItem = workItem,
                    PerWorkSet = workSet,
                    NeedsGrooming = template.IsDataGroomed,
                    DaysRetained = template.DaysRetained,
                    HoursRetained = template.HoursRetained,
                    MinutesRetained = template.MinutesRetained,
                    MonthsRetained = template.MonthsRetained,
                    LastGrooming = DateTime.MinValue,
                    NextGrooming = DateTime.MinValue,
                    Created = DateTime.UtcNow
                };

                try
                {
                    _metadata.Create(instance);

                    if (template.Schedules.EmptyIfNull().Any())
                    {
                        var json = JObject.FromObject(instance);

                        foreach (var schedule in template.Schedules)
                        {
                            AsyncHelper.RunSync(() =>
                                _scheduler.EventScheduleEventsAsync(
                                    schedule, json, null, null, instance.Id.ToString(),
                                    template.Tags)
                            );
                        }
                    }
                } catch(MongoWriteException mex)
                {
                    if (!mex.Message.Contains(DuplicateCollectionException))
                        throw;
                    
                }
                
            }
        }
        

        _initialized = true;    
    }

    protected override IMongoCollection<TableRowData> CreateCollection()
    {
        _initialized.Requires().IsTrue();

        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.KeyRowId));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.KeyDate));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.KeyUser));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.KeyWorkSet));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.KeyWorkItem));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.KeyNumeric));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.Tags));
            collection.AssureIndex(Builders<TableRowData>.IndexKeys.Ascending(it => it.Created));

        });

        return collection;
    }
}
