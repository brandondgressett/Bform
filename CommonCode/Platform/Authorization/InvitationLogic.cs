using BFormDomain.CommonCode.Notification;
using BFormDomain.CommonCode.Platform;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Options;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Authorization;

/// <summary>
/// InvitationLogic manages sending/resending invitations for registration 
///     -References:
///         >Service
///     -Functions:
///         >InviteUser
///         >SendInvitationEmail
///         >ResendInvite
/// </summary>
public class InvitationLogic
{

    private readonly IRepository<InvitationDataModel> _inviteRepo;
    private readonly INotificationCore _notificationCore;
    private readonly IApplicationTerms _appTerms;
    private readonly int _daysToExpire;
    private readonly string _subjectKey;
    private readonly string _bodyKey;


    public InvitationLogic(
        IRepository<InvitationDataModel> inviteRepo,
        INotificationCore nCore,
        IApplicationTerms appTerms,
        IOptions<InvitationLogicOptions> options)
    {
        _inviteRepo = inviteRepo;
        _notificationCore = nCore; 
        _appTerms = appTerms;
        var optionsVal = options.Value;
        _daysToExpire = optionsVal.DaysToExpire;
        _subjectKey = optionsVal.SubjectKey;
        _bodyKey = optionsVal.BodyKey;
    }

    public async Task<Guid> InviteUser(
        string email, string? name,
        IEnumerable<string> roleNames)
    {
        email.Requires().IsNotNullOrEmpty();
        roleNames.Requires().IsNotNull();
        roleNames.Requires().IsNotEmpty();

        var id = Guid.NewGuid();
        var code = GuidEncoder.Encode(Guid.NewGuid())!;
        await _inviteRepo.CreateAsync(
            new InvitationDataModel
            {
                EmailTarget = email,
                Expiration = DateTime.UtcNow + TimeSpan.FromDays(_daysToExpire),
                Id = id,
                InvitationCode = code,
                InvitedRoles = roleNames.ToList(),
                Version = 0
            });

        await SendInvitationEmail(email, name, code);

        return id;
    }

    private async Task SendInvitationEmail(string email, string? name, string code)
    {
        email.Requires().IsNotNullOrEmpty();
        code.Requires().IsNotNullOrEmpty();
        
        var subject = _appTerms.ApplicationTerms[_subjectKey];
        string? body = _appTerms.ApplicationTerms[_bodyKey];
        body = body.Replace("{invitecode}", code);
        if (name is not null)
            body = body.Replace("{name}", name);

        string? htmlBody = null!;
        if (body.StartsWith(BuiltIn.BFormHtml))
        {
            htmlBody = body;
            body = null!;
        }

        await _notificationCore.SendEmail(email, name ?? "user", subject, body, htmlBody);
    }

    public async Task ResendInvite(string originalEmail, string? newEmail, string? name)
    {
        originalEmail.Requires().IsNotNullOrEmpty();
        var (invitation, ctx) = await _inviteRepo.GetOneAsync(it => it.EmailTarget == originalEmail);
        invitation.Requires().IsNotNull();
        if(newEmail is not null)
        {
            invitation!.EmailTarget = newEmail;
            await _inviteRepo.UpdateAsync((invitation, ctx));
        }

        await SendInvitationEmail(invitation!.EmailTarget, name, invitation!.InvitationCode);



    }

}
