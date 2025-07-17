using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.DataModels;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using BFormDomain.CommonCode.Platform.ManagedFiles;

namespace BFormDomain.CommonCode.Platform.Tags;

/// <summary>
/// Tagger manages tagging and untagging items via entity repositories 
///     -References:
///         >FormLogic.cs
///         >AcceptKPIInstanceContent.cs
///         >KPILogic.cs
///         >ManagedFileLogic.cs
///         >ReportGroomningService.cs
///         >ReportLogic.cs
///         >WorkItemLogic.cs
///         >WorkSetLogic.cs
///         >WorkSetAndItemFinder.cs
///     -Functions:
///         >IdsFromTags
///         >CountTags
///         >TagAndCount
///         >Tag
///         >Untag
///         >ReconcileTags
/// </summary>
public class Tagger
{
    private readonly IRepository<TagCountsDataModel> _repo;
    private readonly IApplicationAlert _alerts;


    public Tagger(
        IRepository<TagCountsDataModel> repo,
        IApplicationAlert alerts)
    {
        _repo = repo;
        _alerts = alerts;
    }

    public async Task<IEnumerable<Guid>> IdsFromTags<T>(
        IEnumerable<string> allTags,
        IRepository<T> entityRepo,
        Guid? host = null)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        using (PerfTrack.Stopwatch(nameof(IdsFromTags)))
        {
            var lallTags = TagUtil.MakeTags(allTags).ToArray();
            List<T> matches = null!;
            if (host is null)
                (matches, _) = await entityRepo.GetAllAsync(it => lallTags.All(at => it.Tags.Contains(at)));
            else
            {
                var hostWorkSet = host.Value;
                (matches, _) = await entityRepo.GetAllAsync(it => it.HostWorkSet == hostWorkSet && lallTags.All(at => it.Tags.Contains(at)));
            }
            return matches.Select(it => it.Id);
        }
    }


    public async Task CountTags<T>(
        T item, ITransactionContext trx, 
        int amount, string addTag) 
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        using (PerfTrack.Stopwatch("Count Tags"))
        {

            var existingCountResult = await _repo.GetOneAsync(
                                trx,
                                it =>
                                    it.Tag == addTag &&
                                    it.EntityType == item.EntityType &&
                                    it.TemplateType == item.Template);

            var (existingCount, context) = existingCountResult;

            if (existingCount is null)
            {
                TagCountsDataModel newTag = new()
                {
                    Id = Guid.NewGuid(),
                    Count = 1,
                    EntityType = item.EntityType,
                    TemplateType = item.Template,
                    Tag = addTag,
                    Version = 0
                };

                await _repo.CreateAsync(trx, newTag);
            }
            else
            {
                await _repo.IncrementOneByIdAsync(trx,
                    existingCount.Id, it => it.Count, amount);
            }
        }
    }

    private async Task<bool> CountTags<T>(
        T item,
        bool addTags,
        IEnumerable<string> tags,
        ITransactionContext trx,
        int amount)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        bool changed = false;
        
        using (PerfTrack.Stopwatch("Tag and Count"))
        {

            var etags = item.Tags;
            var running = new List<Task>();
            foreach (var tag in tags)
            {
                var addTag = TagUtil.MakeTag(tag);
                bool process = addTags ? !etags.Contains(addTag) :
                                         etags.Contains(addTag);

                if (!process)
                    continue;


                changed = true;
                if (addTags)
                    etags.Add(tag);
                else
                    etags.Remove(tag);

                running.Add(CountTags(item, trx, amount, addTag));

            }

            await Task.WhenAll(running);
        }

        return changed;
    }


    private async Task<bool> TagAndCount<T>(
        T item, 
        bool addTags,
        IRepository<T>? entityRepo, 
        IEnumerable<string> tags, 
        ITransactionContext trx, 
        int amount) 
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        bool changed = false;
        using (PerfTrack.Stopwatch("Tag and Count"))
        {

            var etags = item.Tags;
            List<string> etagstemp = new();
            foreach (var tag in tags)
            {
                etagstemp.Add(tag);
            }
            var running = new List<Task>();
            foreach (var tag in tags)
            {
                var addTag = TagUtil.MakeTag(tag);
                bool process = addTags ? !etagstemp.Contains(addTag) :
                                         etagstemp.Contains(addTag);

                if (!process)
                    continue;


                changed = true;
                if (addTags)
                    etags.Add(addTag);
                else
                    etags.Remove(addTag);//This line is causing the issue

                if(entityRepo is not null)
                    running.Add(entityRepo.UpdateAsync(trx, item));

                running.Add(CountTags(item, trx, amount, addTag));

            }
            
            tags = etagstemp;

            await Task.WhenAll(running);
        }

        return changed;
    }

    

    public async Task Tag<T>(
        T item, IRepository<T> entityRepo, 
        IEnumerable<string> tags)
        where T: class, IDataModel, ITaggable, IAppEntity
    {
        using var trx = await entityRepo.OpenTransactionAsync();
        
        try
        {
            //trx.Begin();

            bool changed = await TagAndCount(item, true, entityRepo, tags, trx, 1);

            if (changed)
                await trx.CommitAsync();
            else
                await trx.AbortAsync();

        }
        catch (Exception ex)
        {
            try { await trx.AbortAsync(); } catch { }
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot tag entity. {ex.TraceInformation()}",
                1);

        }

    }

    public async Task Untag<T>(
        T item, IRepository<T> entityRepo, 
        IEnumerable<string> tags)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        using var trx = await entityRepo.OpenTransactionAsync();
        
        try
        {
            //trx.Begin();

            bool changed = await TagAndCount(item, false, entityRepo, tags, trx, -1);

            if (changed)
                await trx.CommitAsync();
            else
                await trx.AbortAsync();

        }
        catch (Exception ex)
        {
            try { await trx.AbortAsync(); } catch { }
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot tag entity. {ex.TraceInformation()}",
                1);
        }
    }

    public async Task<bool> Tag<T>(
        ITransactionContext trx,
        T item, IRepository<T> entityRepo,
        IEnumerable<string> tags)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        
        try
        {
           
            bool changed = await TagAndCount(item, true, entityRepo, tags, trx, 1);

            return changed;
        }
        catch (Exception ex)
        {
           
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot tag entity. {ex.TraceInformation()}",
                1);

            throw;

        }

    }

    public async Task<bool> Untag<T>(
        ITransactionContext trx,
        T item, IRepository<T> entityRepo,
        IEnumerable<string> tags)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        
        try
        {
            
            bool changed = await TagAndCount(item, false, entityRepo, tags, trx, -1);

            return changed;

        }
        catch (Exception ex)
        {
            
            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot tag entity. {ex.TraceInformation()}",
                1);
            throw;
        }
    }


    public async Task<bool> ReconcileTags<T>(
        ITransactionContext trx,
        T item,
        IEnumerable<string> setToTags)
        where T : class, IDataModel, ITaggable, IAppEntity
    {
        try
        {
            var existingTags = item.Tags;
            var readyTags = setToTags.Select(tg => TagUtil.MakeTag(tg));
            var newTags = readyTags.Where(st => !existingTags.Contains(st));
            var removedTags = existingTags.Where(et=> !readyTags.Contains(et));

            if (newTags.Any() || removedTags.Any())
            {
                item.Tags.AddRange(newTags);
                item.Tags.RemoveAll(tg => removedTags.Contains(tg));

                await CountTags(item, true, newTags, trx, 1);
                await CountTags(item, false, removedTags, trx, -1);
            }

            return newTags.Any() || removedTags.Any();
        }
        catch (Exception ex)
        {

            _alerts.RaiseAlert(ApplicationAlertKind.Services, LogLevel.Warning,
                $"Cannot tag entity. {ex.TraceInformation()}",
                1);
            throw;
        }
    }

}
