using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;
using System;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems;



public class WorkItemViewModel
{
    public string TemplateName { get; set; } = null!;
    public List<StatusTemplate> StatusTemplates { get; set; } = new();
    public List<TriageTemplate> TriageTemplates { get; set; } = new();
    public List<PriorityTemplate> PriorityTemplates { get; set; } = new();

    public List<SectionViewModel> Sections { get; set; } = new();

    public bool AllowComments { get; set; } = true;
    public bool AllowFileAttachments { get; set; } = true;
    public bool AllowDeletion { get; set; } = true;
    public bool AllowLinks { get; set; } = true;
    public bool AllowBookmarks { get; set; } = true;

    public bool TrackStatus { get; set; } = true;
    public bool TrackAssignee { get; set; } = true;

    public bool TrackPriority { get; set; } = true;
    public bool TrackEventHistory { get; set; } = true;
    public bool TrackUnresolvedLength { get; set; } = true;

    public Guid Id { get; set; }
    public Guid? HostWorkSet { get; set; }

    public List<string> Tags { get; set; } = new List<string>();

    public bool IsListed { get; set; }

    public bool IsVisible { get; set; }

    public Guid? UserAssignee { get; set; }
    public string? UserAssigneeName { get; set; }
    public string? TriageAssignee { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public string? Status { get; set; } = null!;

    public string? Priority { get; set; } = null!;

    public List<WorkItemEventHistoryViewModel> EventHistory { get; set; } = new();
    public List<WorkItemBookmarkViewModel> Bookmarks { get; set; } = new();

    public List<WorkItemLink> Links { get; set; } = new();

    public List<CommentViewModel> Comments { get; set; } = new();

    public List<ManagedFileViewModel> Files { get; set; } = new();

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public string? CreatorName { get; set; }
    public Guid? LastModifier { get; set; }
    public string? LastModifierName { get; set; }


    public static async Task<WorkItemViewModel> Create(
        WorkItemTemplate template, WorkItem item,
        EntityReferenceLoader loader,
        CommentsLogic comments, ManagedFileLogic files,
        UserInformationCache uic, string tzid)
    {
        template.Name.Requires().IsEqualTo(item.Template);
        
        var localTz = TimeZoneInfo.FromSerializedString(tzid);
        string? triageAssignee = null!;
        if (item.TriageAssignee is not null)
        {
            var triage = template.TriageTemplates.First(it => it.TriageId == item.TriageAssignee.Value);
            triageAssignee = triage.Title;
        }

        var status = template.StatusTemplates.First(it => it.Id == item.Status);
        string statusTitle = status.Title;

        var priority = template.PriorityTemplates.First(it => it.Id == item.Priority);
        string priorityTitle = priority.Title;

        string creatorName = "";
        if(item.Creator.HasValue)
        {
            var user = await uic.Fetch(item.Creator.Value);
            if (user is not null)
                creatorName = user.UserName;
        }

        string modifierName = "";
        if(item.LastModifier.HasValue)
        {
            var user = await uic.Fetch(item.LastModifier.Value);
            if(user is not null)
                modifierName = user.UserName;
        }

        var retval = new WorkItemViewModel
        {
            TemplateName = template.Name,
            StatusTemplates = template.StatusTemplates,
            TriageTemplates = template.TriageTemplates,
            PriorityTemplates = template.PriorityTemplates,
            AllowComments = template.AllowComments,
            AllowFileAttachments= template.AllowFileAttachments,
            AllowDeletion = template.AllowDeletion,
            AllowLinks = template.AllowLinks,
            AllowBookmarks = template.AllowBookmarks,
            TrackStatus = template.TrackStatus,
            TrackAssignee = template.TrackAssignee,
            TrackPriority = template.TrackPriority,
            TrackEventHistory = template.TrackEventHistory,
            TrackUnresolvedLength = template.TrackUnresolvedLength,
            Id = item.Id,
            HostWorkSet = item.HostWorkSet,
            Tags = item.Tags,
            IsListed = item.IsListed,
            IsVisible = item.IsVisible,
            UserAssignee = item.UserAssignee,
            TriageAssignee = triageAssignee,
            Title = item.Title,
            Description = item.Description,
            Status = statusTitle,
            Priority = priorityTitle,
            CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(item.CreatedDate, localTz),
            UpdatedDate = TimeZoneInfo.ConvertTimeFromUtc(item.UpdatedDate, localTz),
            Creator = item.Creator,
            LastModifier = item.LastModifier,
            CreatorName = creatorName,
            LastModifierName = modifierName,
            Links = item.Links
        };

        if (item.UserAssignee is not null)
        {
            var user = (await uic.Fetch(item.UserAssignee.Value))!;
            user.Guarantees().IsNotNull();
            retval.UserAssigneeName = user.UserName;
        }

        foreach(var section in item.Sections)
        {
            var sectionTemplate = template.SectionTemplates.First(it => it.Id == section.TemplateId);
            var sectionVM = new SectionViewModel
            {
                Id = section.TemplateId,
                DescendingOrder = sectionTemplate.DescendingOrder,
                EntityTemplateName = sectionTemplate.EntityTemplateName,
                IsCreateOnDemand = sectionTemplate.IsCreateOnDemand,
                IsCreateOnNew = sectionTemplate.IsCreateOnNew,
                IsEntityList = sectionTemplate.IsEntityList,
                Renderer = sectionTemplate.Renderer,
            };

            var work = new List<Task<JObject?>>();
            foreach(var entityUri in section.Entities)
            {
                work.Add(loader.LoadEntityJsonFromReference(entityUri.ToString()));
            }

            await Task.WhenAll(work);
            foreach(var t in work)
            {
                var entityData = t.Result;
                if (entityData is not null)
                    sectionVM.SectionData.Add(entityData);
            }

            retval.Sections.Add(sectionVM);

        }

        foreach(var h in item.EventHistory)
        {
            var assignee = string.Empty;
            if (h.UserAssignee.HasValue)
            {
                var user = await uic.Fetch(h.UserAssignee.Value);
                if (user is not null)
                    assignee = user.UserName;
            }

            var triage = string.Empty;
            if (h.TriageAssignee.HasValue)
            {
                var tt = template.TriageTemplates.First(t => t.TriageId == h.TriageAssignee.Value);
                triage = tt.Title;
            }

            var hpriority = string.Empty;
            var pt = template.PriorityTemplates.FirstOrDefault(it=>it.Id == h.Priority);
            if(pt is not null)
                hpriority = pt.Title;

            var modifier = string.Empty;
            if(h.Modifier.HasValue)
            {
                var user = await uic.Fetch(h.Modifier.Value);
                if(user is not null)
                    modifier = user.UserName;
            }

            var hstatus = string.Empty;
            var st = template.StatusTemplates.FirstOrDefault(it => it.Id == h.Status);
            if(st is not null)
                hstatus = st.Title;
                

            retval.EventHistory.Add(new WorkItemEventHistoryViewModel
            {
                EventTime = DateTime.UtcNow,
                UserAssignee = h.UserAssignee,
                UserAssigneeName = assignee,
                Modifier = h.Modifier,
                ModifierUserName = modifier,
                Priority = h.Priority,
                PriorityTitle = hpriority,
                Status = h.Status,
                StatusTitle = hstatus,
                TriageAssignee = h.TriageAssignee,
                TriageAssigneeTitle = assignee
            });
        }

        foreach(var bm in item.Bookmarks)
        {
            var un = string.Empty;
            var user = await uic.Fetch(bm.ApplicationUser);
            if (user is not null)
                un = user.UserName;

            retval.Bookmarks.Add(new WorkItemBookmarkViewModel
            {
                Created = TimeZoneInfo.ConvertTimeFromUtc(bm.Created, localTz),
                Title = bm.Title,
                ApplicationUser = bm.ApplicationUser,
                UserName = un
            });
        }

        retval.Comments = (await comments.GetEntityComments(item.Id, tzid, 0)).ToList();
        
        var wiFiles = (await files.GetAttachedFiles(item.Id, 0, ManagedFileLogic.Ordering.ModifiedDate)).ToList();
        var wiFileVMs = (await ManagedFileViewModel.Convert(wiFiles, uic, tzid));

        return retval;
    }
}
