using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables;

/// <summary>
/// TableLogic  mananges CRUD operations for table data in the the TableDataRepository
///     -References:
///         >KPIEvaluator.cs
///         >ReportLogic.cs
///         >AcceptTableViewContent.cs
///         >TableEntityLoaderModule.cs
///         >Rule Actions 
///     -Functions:
///         >EventMapInsertTableRow
///         >ActionMapInsertTableRow
///         >EventDeleteTableRow
///         >ActionDeleteTableRow
///         >EventClearTableRows
///         >EventMapEditTableRow
///         >ActionMapEditTableRow
///         >QueryDataTableAll
///         >QueryDataTablePage
///         >QueryDataTableSummary
///         >RegisteredQueryDataTableAll
///         >RegisteredQueryDataTablePage
///         >RegisteredDataTableSummary
///         >GetTemplate
/// </summary>
public class TableLogic
{

    private readonly KeyInject<string, TableDataRepository>.ServiceResolver _repoFactory;
    private readonly IApplicationPlatformContent _content;
    private readonly IApplicationAlert _alerts;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly AppEventSink _eventSink;
    private readonly TemplateNamesCache _tnCache;

    public TableLogic(
        IApplicationAlert alerts,
        IApplicationPlatformContent content,
        IDataEnvironment env,
        AppEventSink sink,
        TemplateNamesCache tnCache,
        KeyInject<string, TableDataRepository>.ServiceResolver repoFactory)

    {
        _alerts = alerts;
        _content = content;
        _tnCache = tnCache;
        _eventSink = sink;
        _dataEnvironment = env;
        _repoFactory = repoFactory;
    }




    public async Task<Guid> EventMapInsertTableRow(
        AppEventOrigin origin,
        string tableTemplate,
        Guid workSet,
        Guid workItem,
        JObject eventData,
        IEnumerable<Mapping> mapping,
        IEnumerable<string>? tags,
        bool seal = false,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventMapInsertTableRow)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            var (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(workSet, workItem);

            try
            {
                Guid id = Guid.NewGuid();

                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                var maps = mapping.ToDictionary(it => it.Field);
                JObject dataRowProperties = ProjectionMapper.Project(eventData, template, maps);

                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);
                var record = new TableRowData { Id = id, Created = DateTime.UtcNow };
                record.SetProperties(dataRowProperties, template);
                if (tags is not null)
                {
                    var readyTags = TagUtil.MakeTags(tags);
                    record.Tags = readyTags.ToList();
                }

                await data.CreateAsync(record);

                _eventSink.BeginBatch(trx!);
                var projectionWrapping = new EntityWrapping<JObject>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = origin.Preceding?.OriginUser,
                    EntityType = nameof(TableRowData),
                    HostWorkItem = workItem,
                    HostWorkSet = workSet,
                    Id = id,
                    Template = tableTemplate,
                    LastModifier = origin.Preceding?.OriginUser,
                    UpdatedDate = DateTime.UtcNow,
                    Payload = dataRowProperties,
                    Tags = record.Tags
                };


                var topic = $"{wsTemplate}.{wiTemplate}.{tableTemplate}.event.table_insert_data";
                await _eventSink.Enqueue(origin, topic, null, projectionWrapping, null, eventTags, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();

                return id;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task<Guid> ActionMapInsertTableRow(
        string action,
        string tableTemplate,
        Guid userId,
        Guid workSet,
        Guid workItem,
        JObject dataSource,
        IEnumerable<string>? tags,
        IEnumerable<Mapping> mapping,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventMapInsertTableRow)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            var (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(workSet, workItem);

            try
            {
                Guid id = Guid.NewGuid();

                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                var maps = mapping.ToDictionary(it => it.Field);
                JObject projection = ProjectionMapper.Project(dataSource, template, maps);


                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);
                var record = new TableRowData { Id = id, Created = DateTime.UtcNow };
                record.SetProperties(projection, template);

                if (tags is not null)
                {
                    var readyTags = TagUtil.MakeTags(tags);
                    record.Tags = readyTags.ToList();
                }

                await data.CreateAsync(record);

                _eventSink.BeginBatch(trx!);
                var projectionWrapping = new EntityWrapping<JObject>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = userId,
                    EntityType = nameof(TableRowData),
                    HostWorkItem = workItem,
                    HostWorkSet = workSet,
                    Id = id,
                    Template = tableTemplate,
                    LastModifier = userId,
                    UpdatedDate = DateTime.UtcNow,
                    Payload = projection,
                    Tags = record.Tags
                };


                var topic = $"{wsTemplate}.{wiTemplate}.{tableTemplate}.action.table_insert_data";
                await _eventSink.Enqueue(null, topic, action, projectionWrapping, userId, null, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();

                return id;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }


    public async Task EventDeleteTableRow(
        AppEventOrigin origin,
        Guid id,
        string tableTemplate,
        Guid workSet,
        Guid workItem,
        bool seal = false,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventMapInsertTableRow)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            var (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(workSet, workItem);

            try
            {


                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);

                await data.DeleteFilterAsync(tr => tr.Id == id);

                _eventSink.BeginBatch(trx!);
                var projectionWrapping = new EntityWrapping<JObject>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = origin.Preceding?.OriginUser,
                    EntityType = nameof(TableRowData),
                    HostWorkItem = workItem,
                    HostWorkSet = workSet,
                    Id = id,
                    Template = tableTemplate,
                    LastModifier = origin.Preceding?.OriginUser,
                    UpdatedDate = DateTime.UtcNow,

                };


                var topic = $"{wsTemplate}.{wiTemplate}.{tableTemplate}.event.table_delete_data";
                await _eventSink.Enqueue(origin, topic, null, projectionWrapping, null, eventTags, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task ActionDeleteTableRow(
        string action,
        string tableTemplate,
        Guid id,
        Guid userId,
        Guid workSet,
        Guid workItem,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventMapInsertTableRow)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            var (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(workSet, workItem);

            try
            {


                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();


                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);


                await data.DeleteFilterAsync(tr => tr.Id == id);

                _eventSink.BeginBatch(trx!);
                var projectionWrapping = new EntityWrapping<JObject>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = userId,
                    EntityType = nameof(TableRowData),
                    HostWorkItem = workItem,
                    HostWorkSet = workSet,
                    Id = id,
                    Template = tableTemplate,
                    LastModifier = userId,
                    UpdatedDate = DateTime.UtcNow,
                    Payload = null,

                };


                var topic = $"{wsTemplate}.{wiTemplate}.{tableTemplate}.action.table_delete_data";
                await _eventSink.Enqueue(null, topic, action, projectionWrapping, userId, null, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task EventClearTableRows(
        AppEventOrigin origin,
        string tableTemplate,
        Guid workSet,
        Guid workItem,
        bool seal = false,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch(nameof(EventClearTableRows)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            var (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(workSet, workItem);

            try
            {
                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);

                await data.DeleteFilterAsync(tr => true);

                _eventSink.BeginBatch(trx!);
                var projectionWrapping = new EntityWrapping<JObject>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = origin.Preceding?.OriginUser,
                    EntityType = nameof(TableRowData),
                    HostWorkItem = workItem,
                    HostWorkSet = workSet,
                    Id = Guid.Empty,
                    Template = tableTemplate,
                    LastModifier = origin.Preceding?.OriginUser,
                    UpdatedDate = DateTime.UtcNow,

                };


                var topic = $"{wsTemplate}.{wiTemplate}.{tableTemplate}.event.table_clear_data";
                await _eventSink.Enqueue(origin, topic, null, projectionWrapping, null, eventTags, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }


    public async Task EventMapEditTableRow(
        AppEventOrigin origin,
        Guid id,
        string tableTemplate,
        Guid workSet,
        Guid workItem,
        JObject eventData,
        IEnumerable<Mapping> mapping,
        bool seal = false,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventMapInsertTableRow)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            var (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(workSet, workItem);

            try
            {


                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                var maps = mapping.ToDictionary(it => it.Field);
                var projection = new JObject();

                foreach (var col in template.Columns)
                {
                    maps.ContainsKey(col.Field).Guarantees().IsTrue();
                    var map = maps[col.Field];

                    var property = eventData.SelectToken(map.Query);
                    if (property is not null)
                    {
                        property.Type.Guarantees($"{col.Field} requires type {col.Type.EnumName()}, but query {map.Query} has selected a token of type {property.Type.EnumName()}").IsEqualTo(col.Type);
                        projection.Add(col.Field, property);
                    }
                    else
                    {
                        map.Nullable.Guarantees($"{col.Field} isn't nullable, but query {map.Query} didn't find a value.").IsTrue();
                        projection.Add(col.Field, null);
                        continue;
                    }
                }

                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);
                var (record, _) = await data.LoadAsync(id);
                if (record is null)
                    return;


                record.SetProperties(projection, template);

                await data.UpdateAsync(record);

                _eventSink.BeginBatch(trx!);
                var projectionWrapping = new EntityWrapping<JObject>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = origin.Preceding?.OriginUser,
                    EntityType = nameof(TableRowData),
                    HostWorkItem = workItem,
                    HostWorkSet = workSet,
                    Id = id,
                    Template = tableTemplate,
                    LastModifier = origin.Preceding?.OriginUser,
                    UpdatedDate = DateTime.UtcNow,
                    Payload = projection,
                };


                var topic = $"{wsTemplate}.{wiTemplate}.{tableTemplate}.event.table_update_data";
                await _eventSink.Enqueue(origin, topic, null, projectionWrapping, null, eventTags, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task ActionMapEditTableRow(
        string action,
        Guid id,
        string tableTemplate,
        Guid userId,
        Guid workSet,
        Guid workItem,
        JObject dataSource,
        IEnumerable<Mapping> mapping,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventMapInsertTableRow)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            var (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(workSet, workItem);

            try
            {


                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                var maps = mapping.ToDictionary(it => it.Field);
                var projection = new JObject();

                foreach (var col in template.Columns)
                {
                    maps.ContainsKey(col.Field).Guarantees().IsTrue();
                    var map = maps[col.Field];

                    var property = dataSource.SelectToken(map.Query);
                    if (property is not null)
                    {
                        property.Type.Guarantees($"{col.Field} requires type {col.Type.EnumName()}, but query {map.Query} has selected a token of type {property.Type.EnumName()}").IsEqualTo(col.Type);
                        projection.Add(col.Field, property);
                    }
                    else
                    {
                        map.Nullable.Guarantees($"{col.Field} isn't nullable, but query {map.Query} didn't find a value.").IsTrue();
                        projection.Add(col.Field, null);
                        continue;
                    }
                }

                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);
                var (record, _) = await data.LoadAsync(id);
                if (record is null)
                    return;
                record.SetProperties(projection, template);

                await data.UpdateAsync(record);

                _eventSink.BeginBatch(trx!);
                var projectionWrapping = new EntityWrapping<JObject>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = userId,
                    EntityType = nameof(TableRowData),
                    HostWorkItem = workItem,
                    HostWorkSet = workSet,
                    Id = id,
                    Template = tableTemplate,
                    LastModifier = userId,
                    UpdatedDate = DateTime.UtcNow,
                    Payload = projection,

                };


                var topic = $"{wsTemplate}.{wiTemplate}.{tableTemplate}.action.table_update_data";
                await _eventSink.Enqueue(null, topic, action, projectionWrapping, userId, null, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task<TableViewModel> QueryDataTableAll(
        string tableTemplate,
        TableQueryCommand query,

        // for workset / workitem specific 
        Guid? workSet = null,
        Guid? workItem = null)
    {
        using (PerfTrack.Stopwatch(nameof(QueryDataTableAll)))
        {
            try
            {
                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                if (template.IsPerWorkItem)
                    workItem.Requires().IsNotNull();
                else
                    workItem = Guid.Empty;

                if (template.IsPerWorkSet)
                    workSet.Requires().IsNotNull();
                else
                    workSet = Guid.Empty;

                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);

                List<TableRowData> queryData = null!;
                var filter = query.MakePredicate();

                switch (query.Ordering)
                {
                    case QueryOrdering.None:
                        (queryData, _) = await data.GetAllAsync(filter);
                        break;

                    case QueryOrdering.Date:
                        (queryData, _) = await data.GetAllOrderedAsync(
                            tr => tr.KeyDate, false, filter);
                        break;

                    case QueryOrdering.DateDescending:
                        (queryData, _) = await data.GetAllOrderedAsync(
                            tr => tr.KeyDate, true, filter);
                        break;

                    case QueryOrdering.Numeric:
                        (queryData, _) = await data.GetAllOrderedAsync(
                            tr => tr.KeyNumeric, false, filter);
                        break;

                    case QueryOrdering.NumericDescending:
                        (queryData, _) = await data.GetAllOrderedAsync(
                            tr => tr.KeyNumeric, true, filter);
                        break;
                }

                queryData = query.ApplyColumnFilters(queryData).ToList();
                return TableViewModel.Create(queryData, template);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());

                throw;
            }


        }
    }

    public async Task<TableViewModel> QueryDataTablePage(
        string tableTemplate,
        TableQueryCommand query,
        int page,

        // for workset / workitem specific 
        Guid? workSet = null,
        Guid? workItem = null)
    {
        using (PerfTrack.Stopwatch(nameof(QueryDataTableAll)))
        {
            try
            {
                page.Requires().IsGreaterOrEqual(0);

                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                if (template.IsPerWorkItem)
                    workItem.Requires().IsNotNull();
                else
                    workItem = Guid.Empty;

                if (template.IsPerWorkSet)
                    workSet.Requires().IsNotNull();
                else
                    workSet = Guid.Empty;

                var data = _repoFactory(TableDataRepository.Scope);
                data.Initialize(template, workSet, workItem);

                List<TableRowData> queryData = null!;
                var filter = query.MakePredicate();

                switch (query.Ordering)
                {
                    case QueryOrdering.None:
                        (queryData, _) = await data.GetPageAsync(page, filter);
                        break;

                    case QueryOrdering.Date:
                        (queryData, _) = await data.GetOrderedPageAsync(
                            tr => tr.KeyDate, false, page, filter);
                        break;

                    case QueryOrdering.DateDescending:
                        (queryData, _) = await data.GetOrderedPageAsync(
                            tr => tr.KeyDate, true, page, filter);
                        break;

                    case QueryOrdering.Numeric:
                        (queryData, _) = await data.GetOrderedPageAsync(
                            tr => tr.KeyNumeric, false, page, filter);
                        break;

                    case QueryOrdering.NumericDescending:
                        (queryData, _) = await data.GetOrderedPageAsync(
                            tr => tr.KeyNumeric, true, page, filter);
                        break;
                }

                queryData = query.ApplyColumnFilters(queryData).ToList();

                return TableViewModel.Create(queryData, template);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());

                throw;
            }


        }
    }

    public async Task<TableSummaryViewModel> QueryDataTableSummary(
        string tableTemplate,
        TableQueryCommand query,
        TableSummarizationCommand summarization,
        Guid? workSet = null,
        Guid? workItem = null)
    {

        using (PerfTrack.Stopwatch(nameof(QueryDataTableSummary)))
        {
            try
            {
                var template = _content.GetContentByName<TableTemplate>(tableTemplate)!;
                template.Guarantees().IsNotNull();

                var tableData = await QueryDataTableAll(tableTemplate, query, workSet, workItem);
                var summary = summarization.Create(tableData, template, query);
                return summary;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());

                throw;
            }


        }

    }


    public async Task<TableViewModel> RegisteredQueryDataTableAll(
        string tableTemplate,
        string registeredQuery,
        // for workset / workitem specific 
        Guid? workSet = null,
        Guid? workItem = null)
    {
        var queryTemplate = _content.GetContentByName<RegisteredTableQueryTemplate>(registeredQuery)!;
        queryTemplate.Guarantees().IsNotNull();
        var query = queryTemplate.Query;

        return await QueryDataTableAll(
            tableTemplate,
            query,
            workSet,
            workItem);
    }

    public async Task<TableViewModel> RegisteredQueryDataTablePage(
        string tableTemplate,
        string registeredQuery,
        int page,
        // for workset / workitem specific 
        Guid? workSet = null,
        Guid? workItem = null)
    {
        var queryTemplate = _content.GetContentByName<RegisteredTableQueryTemplate>(registeredQuery)!;
        queryTemplate.Guarantees().IsNotNull();
        var query = queryTemplate.Query;

        return await QueryDataTablePage(
            tableTemplate,
            query,
            page,
            workSet,
            workItem);
    }

    public async Task<TableSummaryViewModel> RegisteredDataTableSummary(
        string tableTemplate,
        string registeredQuery,
        string registeredSummary,
        Guid? workSet = null,
        Guid? workItem = null)
    {
        var queryTemplate = _content.GetContentByName<RegisteredTableQueryTemplate>(registeredQuery)!;
        queryTemplate.Guarantees().IsNotNull();
        var query = queryTemplate.Query;

        var summaryTemplate = _content.GetContentByName<RegisteredTableSummarizationTemplate>(registeredSummary)!;
        summaryTemplate.Guarantees().IsNotNull();
        var summary = summaryTemplate.Summarization;

        return await QueryDataTableSummary(
            tableTemplate,
            query,
            summary,
            workSet,
            workItem);


    }

    public TableTemplate? GetTemplate(string name)
    {
        return _content.GetContentByName<TableTemplate>(name);
    }

}
