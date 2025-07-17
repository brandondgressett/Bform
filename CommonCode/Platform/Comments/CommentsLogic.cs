using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Comments;


/// <summary>
/// CommentsLogic manages CRUD operations for comments
///     References:
///         >RuleActionCommentCreate.cs
///         >RuleActionCommentDelete.cs
///         >RuleActionCommentEdit.cs
///         >ReportInstanceViewModel.cs
///         >ReportLogic.cs
///         >WorkItemLogic.cs
///         >WorkItemViewModel.cs
///     -Functions:
///         >HelpCreateComment
///         >ActionCreateComment
///         >EventCreateComment
///         >ActionEditComment
///         >EventEditComment
///         >ActionDeleteComment
///         >EventDeleteComment
///         >GetEntityComments
/// </summary>
public class CommentsLogic
{
    #region fields
    private readonly IRepository<Comment> _comments;
    private readonly AppEventSink _eventSink;
    private readonly TemplateNamesCache _tnCache;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly UserInformationCache _userCache;
    private readonly IApplicationAlert _alerts;
    #endregion

    #region ctor
    public CommentsLogic(
        IRepository<Comment> comments,
        AppEventSink sink,
        TemplateNamesCache tnCache,
        IDataEnvironment dataEnv,
        UserInformationCache userCache,
        IApplicationAlert alerts)
    {
        _comments = comments;
        _eventSink = sink;
        _dataEnvironment = dataEnv;
        _userCache = userCache;
        _tnCache = tnCache;
        _alerts = alerts;
    }
    #endregion

    #region creation
    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="action"></param>
    /// <param name="userId"></param>
    /// <param name="workSet"></param>
    /// <param name="workItem"></param>
    /// <param name="hostEntity"></param>
    /// <param name="hostEntityType"></param>
    /// <param name="parentComment"></param>
    /// <param name="commentText"></param>
    /// <param name="trx"></param>
    /// <param name="seal"></param>
    /// <param name="context"></param>
    /// <param name="eventTags"></param>
    /// <returns></returns>
    private async Task<Comment> HelpCreateComment(
        AppEventOrigin? origin,
        string? action,
        Guid? userId,
        Guid workSet,
        Guid workItem,
        Guid hostEntity,
        string hostEntityType,
        Guid? parentComment,
        string commentText,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(HelpCreateComment)))
        {
            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(workSet, workItem);

            var id = Guid.NewGuid();
            Guid commentUser = userId ?? BuiltIn.SystemUser;

            var comment = new Comment
            {
                Id = id,
                CommentText = commentText,
                HostEntity = hostEntity,
                HostType = hostEntityType,
                IsChildComment = parentComment is not null,
                ParentComment = parentComment,
                PostDate = DateTime.UtcNow,
                WorkSet = workSet,
                WorkItem = workItem,
                UserID = commentUser,
                Version = 0
            };

            await _comments.CreateAsync(trx, comment);
            var commentEntity = new EntityWrapping<Comment>
            {
                CreatedDate = DateTime.UtcNow,
                Creator = commentUser,
                HostWorkItem = workItem,
                HostWorkSet = workSet,
                Id = id,
                LastModifier = commentUser,
                Payload = comment,
                UpdatedDate = DateTime.UtcNow

            };

            _eventSink.BeginBatch(trx);
            var topic = $"{workSetTemplateName}.{workItemTemplateName}.{hostEntityType}.{context}.comment_added";
            await _eventSink.Enqueue(origin, topic, action, commentEntity, commentUser, eventTags, seal);
            await _eventSink.CommitBatch();
            return comment;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action"></param>
    /// <param name="userId"></param>
    /// <param name="tzId"></param>
    /// <param name="workSet"></param>
    /// <param name="workItem"></param>
    /// <param name="hostEntity"></param>
    /// <param name="hostEntityType"></param>
    /// <param name="parentComment"></param>
    /// <param name="commentText"></param>
    /// <param name="trx"></param>
    /// <param name="seal"></param>
    /// <returns></returns>
    public async Task<CommentViewModel> ActionCreateComment(
        string action,
        Guid userId,
        string tzId,
        Guid workSet,
        Guid workItem,
        Guid hostEntity,
        string hostEntityType,
        Guid? parentComment,
        string commentText,
        ITransactionContext? trx = null,
        bool seal = false)
    {
        bool isSelfContained = trx is null;
        if (isSelfContained)
            trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

        try
        {

            var comment = await HelpCreateComment(
                null, action, userId, workSet, workItem, hostEntity, hostEntityType,
                parentComment, commentText, trx!, seal, "action", null);
                    
            var vm = (await CommentViewModel.Convert(EnumerableEx.OfOne(comment), _userCache, tzId))
                        .First();

            if(isSelfContained)
                await trx!.CommitAsync();
            return vm;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="asUser"></param>
    /// <param name="workSet"></param>
    /// <param name="workItem"></param>
    /// <param name="hostEntity"></param>
    /// <param name="hostEntityType"></param>
    /// <param name="parentComment"></param>
    /// <param name="commentText"></param>
    /// <param name="trx"></param>
    /// <param name="seal"></param>
    /// <param name="eventTags"></param>
    /// <returns></returns>
    public async Task<Guid> EventCreateComment(
        AppEventOrigin origin,
        Guid? asUser,
        Guid workSet,
        Guid workItem,
        Guid hostEntity,
        string hostEntityType,
        Guid? parentComment,
        string commentText,
        ITransactionContext? trx = null,
        bool seal = false,
        IEnumerable<string>? eventTags = null)
    {
        bool isSelfContained = trx is null;
        if (isSelfContained)
            trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

        try
        {

            var comment = await HelpCreateComment(
                origin, null, asUser, workSet, workItem, hostEntity, hostEntityType,
                parentComment, commentText, trx!, seal, "event", eventTags);

            if (isSelfContained)
                await trx!.CommitAsync();

            return comment.Id;
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
    #endregion

    #region edits
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commentText"></param>
    /// <param name="action"></param>
    /// <param name="userId"></param>
    /// <param name="trx"></param>
    /// <param name="seal"></param>
    /// <returns></returns>
    public async Task ActionEditComment(
        Guid id,
        string commentText,
        string action,
        Guid userId,
        ITransactionContext? trx = null,
        bool seal = false)
    {
        using (PerfTrack.Stopwatch(nameof(ActionEditComment)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (comment, rc) = await _comments.LoadAsync(id);
                comment.Guarantees().IsNotNull();
                comment.CommentText = commentText;

                await _comments.UpdateAsync(trx!, (comment, rc));

                var commentEntity = new EntityWrapping<Comment>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = comment.UserID,
                    HostWorkItem = comment.WorkSet,
                    HostWorkSet = comment.WorkItem,
                    Id = id,
                    LastModifier = comment.UserID,
                    Payload = comment,
                    UpdatedDate = DateTime.UtcNow,

                };


                _eventSink.BeginBatch(trx!);
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(comment.WorkSet, comment.WorkItem);
                var topic = $"{workSetTemplateName}.{workItemTemplateName}.{comment.HostType}.action.comment_edited";
                await _eventSink.Enqueue(null, topic, action, commentEntity, comment.UserID, null, seal);
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commentText"></param>
    /// <param name="origin"></param>
    /// <param name="trx"></param>
    /// <param name="seal"></param>
    /// <param name="eventTags"></param>
    /// <returns></returns>
    public async Task EventEditComment(
        Guid id,
        string commentText,
        AppEventOrigin origin,
        ITransactionContext? trx = null,
        bool seal = false,
        IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventEditComment)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (comment, rc) = await _comments.LoadAsync(id);
                comment.Guarantees().IsNotNull();

                await _comments.UpdateAsync(trx!, (comment, rc));

                var commentEntity = new EntityWrapping<Comment>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = comment.UserID,
                    HostWorkItem = comment.WorkSet,
                    HostWorkSet = comment.WorkItem,
                    Id = id,
                    LastModifier = comment.UserID,
                    Payload = comment,
                    UpdatedDate = DateTime.UtcNow

                };


                _eventSink.BeginBatch(trx!);
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(comment.WorkSet, comment.WorkItem);
                var topic = $"{workSetTemplateName}.{workItemTemplateName}.{comment.HostType}.event.comment_edited";
                await _eventSink.Enqueue(origin, topic, null, commentEntity, comment.UserID, eventTags, seal);
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
    #endregion

    #region deletes
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="action"></param>
    /// <param name="userId"></param>
    /// <param name="trx"></param>
    /// <param name="seal"></param>
    /// <returns></returns>
    public async Task ActionDeleteComment(
        Guid id, 
        string action, 
        Guid userId, 
        ITransactionContext? trx = null,
        bool seal = false)
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteComment)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (comment, rc) = await _comments.LoadAsync(id);
                comment.Guarantees().IsNotNull();

                await _comments.DeleteAsync(trx!, (comment, rc));

                var commentEntity = new EntityWrapping<Comment>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = comment.UserID,
                    HostWorkItem = comment.WorkSet,
                    HostWorkSet = comment.WorkItem,
                    Id = id,
                    LastModifier = comment.UserID,
                    Payload = comment,
                    UpdatedDate = DateTime.UtcNow

                };


                _eventSink.BeginBatch(trx!);
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(comment.WorkSet, comment.WorkItem);
                var topic = $"{workSetTemplateName}.{workItemTemplateName}.{comment.HostType}.action.comment_deleted";
                await _eventSink.Enqueue(null, topic, action, commentEntity, comment.UserID, null, seal);
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="origin"></param>
    /// <param name="trx"></param>
    /// <param name="seal"></param>
    /// <param name="eventTags"></param>
    /// <returns></returns>
    public async Task EventDeleteComment(
        Guid id,
        AppEventOrigin origin,
        ITransactionContext? trx = null,
        bool seal = false,
        IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteComment)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (comment, rc) = await _comments.LoadAsync(id);
                comment.Guarantees().IsNotNull();

                await _comments.DeleteAsync(trx!, (comment, rc));

                var commentEntity = new EntityWrapping<Comment>
                {
                    CreatedDate = DateTime.UtcNow,
                    Creator = comment.UserID,
                    HostWorkItem = comment.WorkSet,
                    HostWorkSet = comment.WorkItem,
                    Id = id,
                    LastModifier = comment.UserID,
                    Payload = comment,
                    UpdatedDate = DateTime.UtcNow

                };


                _eventSink.BeginBatch(trx!);
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(comment.WorkSet, comment.WorkItem);
                var topic = $"{workSetTemplateName}.{workItemTemplateName}.{comment.HostType}.event.comment_deleted";
                await _eventSink.Enqueue(origin, topic, null, commentEntity, comment.UserID, eventTags, seal);
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

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="tzid"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    #region retrieve
    public async Task<IList<CommentViewModel>> GetEntityComments(Guid entityId, string tzid, int page)
    {
        using(PerfTrack.Stopwatch(nameof(GetEntityComments)))
        {
            var (comments, _) = await _comments
                .GetOrderedPageAsync(comment => comment.PostDate, true, page,
                                     c => c.HostEntity == entityId);

            return await CommentViewModel.Convert(comments, _userCache, tzid);
        }
    }
    #endregion

}
