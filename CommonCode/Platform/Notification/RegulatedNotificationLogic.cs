using BFormDomain.CommonCode.Logic.ConsolidateDigest;
using BFormDomain.CommonCode.Logic.DuplicateSuppression;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification;


/// <summary>
/// RegulatedNotificationLogic is used by the NotificationService to manage all forms of notifications including a notification suppression system and a notification digest system
///     -References:
///         >NotificationService.cs
///     -Functions:
///         >Initialize
///         >Notify
///         >SendText
///         >SendEmail
///         >SendCall
///         >SendToast
///         >GroupsNotify
///         >ContactNotify
///         >AcceptDigestParameters
///         >DigestReceiver_DigestReady
///         >SuppresserReceiver_ItemSuppressed
///         >SuppresserReceiver_ItemAllowed
///         >Dispose
/// </summary>
public class RegulatedNotificationLogic : IDisposable, IRegulatedNotificationLogic
{


    private readonly ILogger<RegulatedNotificationLogic> _logger;
    private readonly IRepository<NotificationGroup> _groupRepo;
    private readonly IRepository<NotificationContact> _contactRepo;
    private readonly IRepository<NotificationAudit> _auditRepo;
    private readonly INotificationCore _notificationCore;
    private readonly SuppressionOrder<Suppressible<ExecuteNotifyCommand>> _suppresser;
    private readonly SuppressionResultReceiver<Suppressible<ExecuteNotifyCommand>> _suppresserReceiver;
    private readonly ConsolidateToDigestOrder<Digestible<ExecuteNotifyCommand>> _digester;
    private readonly DigestResultReceiver<ExecuteNotifyCommand> _digestReceiver;

#pragma warning disable IDE0052 // Remove unread private members
    private readonly int _groupNotFoundErrorThreshold;
    private readonly int _notificationContactNotFoundErrorThreshold;
    private readonly int _sendErrorThreshold;
#pragma warning restore IDE0052 // Remove unread private members

    private readonly int _defaultDigestHeadLength;
    private readonly int _defaultDigestTailLength;
    private readonly int _maxEmailDigestItems;


    public RegulatedNotificationLogic(
        IOptions<RegulatedNotificationOptions> options,
        ILogger<RegulatedNotificationLogic> logger,
        IRepository<NotificationGroup> groupRepo,
        IRepository<NotificationContact> contactRepo,
        IRepository<NotificationAudit> auditRepo,
        INotificationCore notificationCore,
        SuppressionOrder<Suppressible<ExecuteNotifyCommand>> suppresser,
        SuppressionResultReceiver<Suppressible<ExecuteNotifyCommand>> suppressionReceiver,
        ConsolidateToDigestOrder<Digestible<ExecuteNotifyCommand>> digester,
        DigestResultReceiver<ExecuteNotifyCommand> digestReceiver)
    {

        _logger = logger;
        _groupRepo = groupRepo;
        _contactRepo = contactRepo;
        _auditRepo = auditRepo;
        _notificationCore = notificationCore;
        _suppresser = suppresser;
        _suppresserReceiver = suppressionReceiver;
        _digester = digester;
        _digestReceiver = digestReceiver;

        var opt = options.Value;
        _groupNotFoundErrorThreshold = opt.GroupNotFoundErrorThreshold;
        _notificationContactNotFoundErrorThreshold = opt.NotificationContactNotFoundErrorThreshold;
        _defaultDigestHeadLength = opt.DefaultDigestHeadLength;
        _defaultDigestTailLength = opt.DefaultDigestTailLength;
        _sendErrorThreshold = opt.SendNotificationErrorThreshold;
        _maxEmailDigestItems = opt.MaxEmailDigestItems;

    }

    public void Initialize()
    {
        _suppresser.Initialize(true);
        _suppresserReceiver.Initialize(true);
        _digestReceiver.Initialize();

        _suppresserReceiver.ItemAllowed += SuppresserReceiver_ItemAllowed;
        _suppresserReceiver.ItemSuppressed += SuppresserReceiver_ItemSuppressed;
        _digestReceiver.DigestReady += DigestReceiver_DigestReady;
    }

    public async Task Notify(NotificationMessage msg)
    {
        _logger.LogInformation("Regulated notification logic received request: {msg}", msg);

        msg.Requires().IsNotNull();
        bool hasTarget =
            msg.NotificationGroups.Any() ||
            msg.NotificationGroup.HasValue ||
            msg.NotificationContact.HasValue; 
            
        hasTarget.Requires().IsTrue();

        using (PerfTrack.Stopwatch("Regulated Notify"))
        {
            if (msg.NotificationGroups.Any())
                await GroupsNotify(msg);
            else if (msg.NotificationGroup.HasValue)
                await GroupNotify(msg, msg.NotificationGroup.Value);
            else if (msg.NotificationContact.HasValue)
                await ContactNotify(msg, msg.NotificationContact.Value);
        }
    }



    private async Task SendText(NotificationContact contact, NotificationMessage msg, bool auditItem = true)
    {
        try
        {
            await _notificationCore.SendText(contact.TextNumber!, msg.SMSText!);
            if(auditItem)
                await _auditRepo.CreateAsync(new NotificationAudit(contact.UserRef,
                    new NotificationAuditEvent
                    {
                        DateTime = DateTime.UtcNow,
                        Body = msg.SMSText ?? "none",
                        ChannelType = ChannelType.Text,
                        Subject = msg.Subject,
                        Severity = msg.Severity,
                    }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{trace}", ex.TraceInformation());


        }
    }

    private async Task SendEmail(NotificationContact contact, NotificationMessage msg, bool auditItem = true)
    {
        try
        {
            await _notificationCore.SendEmail(contact.EmailAddress!, contact.ContactTitle,
                            msg.Subject, msg.EmailText, msg.EmailHtmlText);
            if (auditItem)
                await _auditRepo.CreateAsync(new NotificationAudit(contact.UserRef,
                    new NotificationAuditEvent
                    {
                        DateTime = DateTime.UtcNow,
                        Body = msg.EmailHtmlText ?? msg.EmailText ?? "none",
                        ChannelType = ChannelType.Email,
                        Subject = msg.Subject,
                        Severity=msg.Severity
                    }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{trace}", ex.TraceInformation());


        }
    }

    private async Task SendCall(NotificationContact contact, NotificationMessage msg, bool auditItem = true)
    {
        try
        {
            await _notificationCore.SendCall(contact.CallNumber!, msg.CallText!);
            if (auditItem)
                await _auditRepo.CreateAsync(new NotificationAudit(contact.UserRef,
                    new NotificationAuditEvent
                    {
                        DateTime = DateTime.UtcNow,
                        Body = msg.CallText ?? "none",
                        ChannelType = ChannelType.Call,
                        Subject = msg.Subject, 
                        Severity = msg.Severity
                    }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{trace}", ex.TraceInformation());


        }
    }

    private async Task SendToast(NotificationContact contact, NotificationMessage msg, bool auditItem = true)
    {
        try
        {
            await _notificationCore.SendToast(contact.UserRef, msg.Subject, msg.ToastText!);

            if (auditItem)
                await _auditRepo.CreateAsync(new NotificationAudit(contact.UserRef,
                    new NotificationAuditEvent
                    {
                        DateTime = DateTime.UtcNow,
                        Body = msg.ToastText ?? "none",
                        ChannelType = ChannelType.Toast,
                        Subject = msg.Subject,
                        Severity = msg.Severity
                    }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{trace}", ex.TraceInformation());


        }
    }

    
    private async Task GroupsNotify(NotificationMessage msg)
    {
        msg.NotificationGroups.Requires().IsNotEmpty();
        await Task.WhenAll(msg.NotificationGroups.Select(id => GroupNotify(msg, id)));
    }

    private async Task GroupNotify(
        NotificationMessage msg,
        Guid notificationGroupId)
    {
        msg.Requires().IsNotNull();
        notificationGroupId.Requires().IsNotEqualTo(Guid.Empty);

        bool channelSelected =
            msg.SMSText is not null ||
            msg.EmailText is not null ||
            msg.EmailHtmlText is not null ||
            msg.CallText is not null ||
            msg.ToastText is not null;
        channelSelected.Requires().IsNotEqualTo(false);

        var (group, _) = await _groupRepo.LoadAsync(msg.NotificationGroup!.Value);
        if (group is null)
        {
            _logger.LogWarning("Notification contact group not found, cannot be notified: {msg.NotificationGroup!}",
                msg.NotificationGroup);
        }

        group.Guarantees().IsNotNull();
        group!.Active.Guarantees().IsTrue();

        var (contacts, _) = await _contactRepo.LoadManyAsync(group!.Members.Select(it => it.Id));
        if (contacts.Any())
        {
            var tasks = contacts
                .Select(contact => ContactNotify(msg, contact))
                .ToArray();

            await Task.WhenAll(tasks);

        }
    }

    private async Task ContactNotify(
        NotificationMessage msg,
        Guid notificationContactId)
    {
        notificationContactId.Requires().IsNotEqualTo(Guid.Empty);
        bool channelSelected =
            msg.SMSText is not null ||
            msg.EmailText is not null ||
            msg.EmailHtmlText is not null ||
            msg.CallText is not null;
        channelSelected.Requires().IsNotEqualTo(false);

        var (contact, _) = await _contactRepo.LoadAsync(notificationContactId);
        if (contact is null)
        {
            _logger.LogWarning(
                "Notification Contact not found, cannot be notified: {notificationContactId}",
                notificationContactId);
        }

        if (null != contact && contact.Active)
            await ContactNotify(msg,contact);
    }

    private async Task DoNotificationByRoute(
            NotificationMessage message,
            ChannelRegulation regulation,
            ChannelType channelType,
            NotificationContact nContact,
            DateTime nDigestUntilTime,
            Func<NotificationContact, NotificationMessage, bool, Task> action)
    {
        ChannelRegulation route = ChannelRegulation.Allow;
        if (message.WantSuppression)
            route = ChannelRegulation.Suppress;
        if (message.WantDigest)
            route = ChannelRegulation.Digest;
        if (message.WantSuppression && message.WantDigest)
            route = ChannelRegulation.DigestSuppressed;

        if (regulation > route)
            route = regulation;

        var exec = new ExecuteNotifyCommand
        {
            NotificationMessage = message,
            Contact = nContact,
            ExecuteChannel = channelType
        };

        bool doDigest = false;
        switch (route)
        {
            case ChannelRegulation.Allow:
                await action(nContact, message, true);
                break;
            case ChannelRegulation.Suppress:
                await _suppresser.MaybeSuppressAsync(
                    new Suppressible<ExecuteNotifyCommand>(
                        exec,
                        message.SuppressionMinutes,
                        message.Subject,
                        ne => ne.NotificationMessage!.Subject,
                        ne => ne.NotificationMessage!.CreatorId ?? "none",
                        ne => nContact.UserRef.ToString(),
                        ne => nContact.Id.ToString()
                    ));
                break;
            case ChannelRegulation.Digest:
                doDigest = true;

                break;
            case ChannelRegulation.DigestSuppressed:
                exec.DigestSuppressed = true;
                doDigest = true;
                break;
        }

        if (doDigest)
            await _digester.ConsolidateIntoDigestAsync(
                    new Digestible<ExecuteNotifyCommand>(
                        exec, nDigestUntilTime, message.DigestHead, message.DigestTail,
                        message.Subject,
                       
                        ne => ne.NotificationMessage!.Subject,
                        ne => ne.NotificationMessage!.CreatorId ?? "none",
                        ne => nContact.UserRef.ToString(),
                        ne => nContact.Id.ToString()
                    ));

    }

    private async Task ContactNotify(
        NotificationMessage msg,
        NotificationContact contact)
    {
        bool channelSelected =
             msg.SMSText is not null ||
             msg.EmailText is not null ||
             msg.EmailHtmlText is not null ||
             msg.CallText is not null;

        channelSelected.Requires().IsNotEqualTo(false);

        contact.Requires().IsNotNull();
        contact.Active.Requires().IsNotEqualTo(false);

        var sevContactRules = contact.TimeSeverityTable.BySeverity(msg.Severity);
        var tzi = TimeZoneInfo.FromSerializedString(contact.TimeZoneInfoId);
        var timeContactRules = sevContactRules.ByTime(tzi);


        DateTime digestUntilTime = DateTime.MinValue;
        if (msg.WantDigest)
            digestUntilTime = AcceptDigestParameters(msg, tzi);

        if (msg.SMSText is not null && !string.IsNullOrWhiteSpace(contact.TextNumber))
            await DoNotificationByRoute(msg, timeContactRules.Text, ChannelType.Text, contact, digestUntilTime, SendText);
       
        var finalEmailText = msg.EmailHtmlText ?? msg.EmailText;
        if (finalEmailText is not null && !string.IsNullOrWhiteSpace(contact.EmailAddress))
            await DoNotificationByRoute(msg, timeContactRules.Email, ChannelType.Email, contact, digestUntilTime, SendEmail);
       
        if (msg.CallText is not null && !string.IsNullOrWhiteSpace(contact.CallNumber))
            await DoNotificationByRoute(msg, timeContactRules.Call, ChannelType.Call, contact, digestUntilTime, SendCall);

        if (msg.ToastText is not null)
            await DoNotificationByRoute(msg, timeContactRules.Toast, ChannelType.Toast, contact, digestUntilTime, SendToast); 
       
    }

    private DateTime AcceptDigestParameters(NotificationMessage msg, TimeZoneInfo tzi)
    {
        if (msg.DigestMinutes <= 0)
            msg.DigestMinutes = (int)TimeShifts.TimeUntilNextShift(tzi).TotalMinutes;
        var digestUntilTime = DateTime.UtcNow + TimeSpan.FromMinutes(msg.DigestMinutes);

        if (msg.DigestHead <= 0)
            msg.DigestHead = _defaultDigestHeadLength;
        if (msg.DigestTail <= 0)
            msg.DigestTail = _defaultDigestTailLength;
        return digestUntilTime;
    }

    private async void DigestReceiver_DigestReady(object? sender,
       DigestReadyEventArgs<ExecuteNotifyCommand> e)
    {
        try
        {
            using (PerfTrack.Stopwatch("Receive notification digest."))
            {
                var digested = e.Digest;
                var tasks = new List<Task>();

                var texts = digested.Where(it => it.Item!.ExecuteChannel == ChannelType.Text).ToList();
                if (texts.Any()) TextDigest(texts, tasks);

                var emails = digested.Where(it => it.Item!.ExecuteChannel == ChannelType.Email).ToList();
                if (emails.Any()) EmailDigest(emails, tasks);

                var calls = digested.Where(it => it.Item!.ExecuteChannel == ChannelType.Call).ToList();
                if (calls.Any()) CallDigest(calls, tasks);

                var toasts = digested.Where(it => it.Item!.ExecuteChannel == ChannelType.Toast).ToList();
                if(toasts.Any()) ToastDigest(toasts, tasks);


                await Task.WhenAll(tasks);

                // Create audit for entire digest.
                var auditItems = new List<NotificationAuditEvent>();
                foreach(var digestItem in digested)
                {
                    string body = "none";
                    var dcmd = digestItem!.Item!;
                    var msg = dcmd!.NotificationMessage;

                    switch(digestItem!.Item!.ExecuteChannel)
                    {
                        case ChannelType.Email:
                            body = msg!.EmailHtmlText ?? msg!.EmailText ?? "none";
                            break;
                        case ChannelType.Call:
                            body = msg!.CallText ?? "none";
                            break;
                        case ChannelType.Text:
                            body = msg!.SMSText ?? "none";
                            break;
                        case ChannelType.Toast:
                            body = msg!.ToastText ?? "none";
                            break;
                    }

                    auditItems.Add(new NotificationAuditEvent
                    {
                         Body = body,
                         ChannelType = dcmd.ExecuteChannel,
                         DateTime = digestItem.DateTime,
                         Subject = msg!.Subject,
                         Severity = msg!.Severity
                    });
                }

                var example = digested.Last();
                var cmd = example.Item;

                await _auditRepo.CreateAsync(new NotificationAudit(cmd!.Contact!.UserRef,
                    auditItems.ToArray()));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{trace}", ex.TraceInformation());
        }
    }

    private void CallDigest(List<DigestItem<ExecuteNotifyCommand>> calls, List<Task> tasks)
    {
        var example = calls.Last();
        var cmd = example.Item;
        var msg = cmd!.NotificationMessage;
        var contact = cmd.Contact;

        var callMessage = $"Received {calls.Count} calls about '{msg!.Subject}. last detail: {msg.CallText}";

        var digestMessage = new NotificationMessage
        {
            NotificationGroup = msg.NotificationGroup,
            NotificationContact = msg.NotificationContact,
            CreatorId = msg.CreatorId,
            Subject = msg.Subject,
            Severity = msg.Severity,
            CallText = callMessage
        };

        tasks.Add(SendCall(contact!, digestMessage, auditItem: false));
    }

    private void ToastDigest(List<DigestItem<ExecuteNotifyCommand>> toasts, List<Task> tasks)
    {
        var example = toasts.Last();
        var cmd = example.Item;
        var msg = cmd!.NotificationMessage;
        var contact = cmd.Contact;

        var toastMessage = $"Received {toasts.Count} calls about '{msg!.Subject}. last detail: {msg.ToastText}";

        var digestMessage = new NotificationMessage
        {
            NotificationGroup = msg.NotificationGroup,
            NotificationContact = msg.NotificationContact,
            CreatorId = msg.CreatorId,
            Subject = msg.Subject,
            Severity = msg.Severity,
            ToastText = toastMessage
        };

        tasks.Add(SendToast(contact!, digestMessage, auditItem: false));
    }

    private void EmailDigest(List<DigestItem<ExecuteNotifyCommand>> emails, List<Task> tasks)
    {
        var example = emails.First();
        var cmd = example.Item;
        var msg = cmd!.NotificationMessage;
        var contact = cmd.Contact!;

        var tzi = TimeZoneInfo.FromSerializedString(contact!.TimeZoneInfoId);
        var sb = new StringBuilder();

        sb.AppendLine($"Received {emails.Count} text messages about '{msg!.Subject}'.");
        sb.AppendLine();
        sb.AppendLine();

        bool htmlMode = false;
        int itemCount = 0;
        foreach (var dig in emails)
        {
            var itemTime = TimeZoneInfo.ConvertTimeFromUtc(dig.DateTime, tzi);
            if (!string.IsNullOrWhiteSpace(dig.Item!.NotificationMessage!.EmailHtmlText))
                htmlMode = true;

            var itemText = dig.Item!.NotificationMessage!.EmailHtmlText ?? dig.Item.NotificationMessage!.EmailText;
            sb.AppendLine($"{itemTime.ToShortDateString()} {itemTime.ToShortTimeString()} {msg.Subject}:");
            sb.AppendLine($"{itemText}");
            sb.AppendLine();
            sb.AppendLine();

            itemCount += 1;
            if (itemCount >= _maxEmailDigestItems)
            {
                sb.AppendLine($"Digest list cut to {_maxEmailDigestItems} for email.");
                break;
            }
        }

        var digestMessage = new NotificationMessage
        {
            NotificationGroup = msg.NotificationGroup,
            NotificationContact = msg.NotificationContact,
            CreatorId = msg.CreatorId,
            Subject = msg.Subject,
            Severity = msg.Severity,
            EmailHtmlText = htmlMode ? sb.ToString() : null,
            EmailText = htmlMode ? null : sb.ToString()
        };

        tasks.Add(SendEmail(contact, digestMessage, auditItem: false));
    }

    private void TextDigest(List<DigestItem<ExecuteNotifyCommand>> texts, List<Task> tasks)
    {
        var last = texts.Last();
        var cmd = last.Item;
        var msg = cmd!.NotificationMessage;
        var contact = cmd.Contact!;
        var textMessage = $"Received {texts.Count} text messages about '{msg!.Subject}'. last detail: {msg.SMSText}";

        var digestMessage = new NotificationMessage
        {
            NotificationGroup = msg.NotificationGroup,
            NotificationContact = msg.NotificationContact,
            CreatorId = msg.CreatorId,
            Subject = msg.Subject,
            Severity = msg.Severity,
            SMSText = textMessage,
        };

        tasks.Add(SendText(contact, digestMessage, auditItem: false));
    }

    private async void SuppresserReceiver_ItemSuppressed(object? sender,
        ItemSuppressedEventArgs<Suppressible<ExecuteNotifyCommand>> e)
    {
        try
        {
            var suppressed = e.Item;
            var ne = suppressed!.Item!;

            if (ne.DigestSuppressed)
            {
                var msg = ne.NotificationMessage;
                var contact = ne.Contact;
                var tzi = TimeZoneInfo.FromSerializedString(contact!.TimeZoneInfoId);
                DateTime digestUntilTime = AcceptDigestParameters(msg!, tzi);

                await _digester.ConsolidateIntoDigestAsync(
                        new Digestible<ExecuteNotifyCommand>(
                            ne, digestUntilTime, msg!.DigestHead, msg.DigestTail,
                            msg.Subject,
                            
                            ne => ne.NotificationMessage!.Subject,
                            ne => ne.NotificationMessage!.CreatorId ?? "none",
                            ne => contact.UserRef.ToString(),
                            ne => contact.Id.ToString()
                        ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{trace}", ex.TraceInformation());
        }

    }

    private async void SuppresserReceiver_ItemAllowed(object? sender,
        ItemAllowedEventArgs<Suppressible<ExecuteNotifyCommand>> e)
    {
        try
        {
            var suppressed = e.Item;
            var ne = suppressed!.Item;

            switch (ne!.ExecuteChannel)
            {
                case ChannelType.Text:
                    await SendText(ne.Contact!, ne.NotificationMessage!);
                    break;
                case ChannelType.Email:
                    await SendEmail(ne.Contact!, ne.NotificationMessage!);
                    break;
                case ChannelType.Call:
                    await SendCall(ne.Contact!, ne.NotificationMessage!);
                    break;
                case ChannelType.Toast:
                    await SendToast(ne.Contact!, ne.NotificationMessage!);
                    break;

            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{trace}", ex.TraceInformation());
        }

    }


    public void Dispose()
    {
        if (_suppresserReceiver is not null)
        {
            _suppresserReceiver.ItemAllowed -= SuppresserReceiver_ItemAllowed;
            _suppresserReceiver.ItemSuppressed -= SuppresserReceiver_ItemSuppressed;
        }

        if (_digestReceiver is not null)
            _digestReceiver.DigestReady -= DigestReceiver_DigestReady;

        _suppresserReceiver?.Dispose();
        _digestReceiver?.Dispose();

        GC.SuppressFinalize(this);
    }

}

