using BFormDomain.Diagnostics;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification;



/// <summary>
/// TwilioNotificationCore utilizes the Twlio API to make calls, send text messages, send emails, and send toast notifications. 
/// TwilioNotificationCore implements all functions from INotificationCore
///     -References:
///         >InvitationLogic.cs
///         >UserManagementLogic.cs
///         >RegulatedNotificationLogic.cs
///     -Functions:
///         >SendEmail
///         >SendText
///         >SendCall
///         >SendToast
/// </summary>
public class TwilioNotificationCore : INotificationCore
{
    #region fields
    private readonly string _sendGridKey;
    private readonly string _twilioSid;
    private readonly string _twilioToken;

    private readonly ILogger<TwilioNotificationCore> _logger;
    private readonly SendGridClient _sendGridClient;
    private readonly string _fromEmail;
    private readonly string _fromEmailName;
    private readonly EmailAddress _fromEmailAddress;
    private readonly PhoneNumber _fromPhoneNumber;
    private readonly UserToastLogic _userToasts;

#pragma warning disable IDE0052 // Remove unread private members
    private readonly int _failureThreshold = 50;
#pragma warning restore IDE0052 // Remove unread private members
    #endregion

    public TwilioNotificationCore(
        UserToastLogic userToasts,
        ILogger<TwilioNotificationCore> logger,
        IOptions<TwilioNotificationOptions> options)
    {
        _userToasts = userToasts; 
        var optionsVal = options.Value;
        _sendGridKey = optionsVal.SendGridKey;
        _twilioSid = optionsVal.TwilioSid;
        _twilioToken = optionsVal.TwilioToken;
        _fromPhoneNumber = new PhoneNumber(optionsVal.FromPhoneNumber);
        _failureThreshold = optionsVal.FailureThreshold;

        _fromEmail = optionsVal.FromEmail;
        _fromEmailName = optionsVal.FromEmailName;
        _fromEmailAddress = new EmailAddress(_fromEmail, _fromEmailName);

        
        _logger = logger;

        _sendGridClient = new SendGridClient(_sendGridKey);
        TwilioClient.Init(_twilioSid, _twilioToken);
    }

    

    public async Task SendEmail(
        string emailAddress, string emailName,
        string subject,
        string? plainTextContent = null, string? htmlContent = null)
    {
        using (PerfTrack.Stopwatch("SendGrid Send Email"))
        {
            // TODO: validate email address formats.

            emailAddress.Requires().IsNotNullOrEmpty();
            emailName.Requires().IsNotNullOrEmpty();
            subject.Requires().IsNotNullOrEmpty();

            int contentCount = (string.IsNullOrWhiteSpace(plainTextContent) ? 1 : 0) +
                               (string.IsNullOrWhiteSpace(htmlContent) ? 1 : 0);
            contentCount.Requires("provide either text or html content for the email body.").IsEqualTo(1);

            _logger.LogInformation("emailing {emailAddress} -> {subject}", emailAddress, subject);

            var recipient = new EmailAddress(emailAddress, emailName);
            var msg = MailHelper.CreateSingleEmail(_fromEmailAddress, recipient, subject, plainTextContent, htmlContent);
            var response = await _sendGridClient.SendEmailAsync(msg);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("email to {emailAddress} failed: {statusCode}", 
                    emailAddress, response.StatusCode);
            }

            response.IsSuccessStatusCode.Guarantees("Failed to send email.").IsTrue();
        }
    }


    public async Task SendText(string textPhoneNumber, string textBody)
    {
        using (PerfTrack.Stopwatch("Twilio Send Text"))
        {
            // TODO: validate phone number format. eg, +11234567890
            textPhoneNumber.Requires().IsNotNullOrEmpty();
            textBody.Requires().IsNotNullOrEmpty();

            _logger.LogInformation("texting {textPhoneNumber}: {textBody}", textPhoneNumber, textBody);

            var messageTo = new PhoneNumber(textPhoneNumber);
            var text = await MessageResource.CreateAsync(messageTo, from: _fromPhoneNumber, body: textBody);
            bool failed =
               text.Status == MessageResource.StatusEnum.Canceled ||
               text.Status == MessageResource.StatusEnum.Failed ||
               text.Status == MessageResource.StatusEnum.Undelivered;

            if (failed)
            {
               _logger.LogWarning(
                    "text to {textPhoneNumber} failed: {textErrorCode}- {textErrorMessage}",
                    textPhoneNumber, text.ErrorCode, text.ErrorMessage ?? "unknown");
            }

            failed.Guarantees(text.ErrorMessage ?? "Unknown problem delivering text.").IsFalse();
        }
    }


    public async Task SendCall(string callPhoneNumber, string sayThis)
    {
        using (PerfTrack.Stopwatch("Twilio Send Call"))
        {
            // TODO: validate phone number format. eg, +11234567890
            callPhoneNumber.Requires().IsNotNullOrEmpty();
            sayThis.Requires().IsNotNullOrEmpty();

            _logger.LogInformation("calling {callPhoneNumber} to say: '{sayThis}'", callPhoneNumber, sayThis);

            string callBody = $"<Response><Say>{sayThis}</Say><Hangup/></Response>";
            var callTo = new PhoneNumber(callPhoneNumber);
            var call = await CallResource.CreateAsync(to: callTo, from: _fromPhoneNumber, twiml: callBody);
            bool failed =
               call.Status == MessageResource.StatusEnum.Canceled ||
               call.Status == MessageResource.StatusEnum.Failed ||
               call.Status == MessageResource.StatusEnum.Undelivered;

            if (failed)
            {
                _logger.LogWarning(
                    "call to {callPhoneNumber} to say {sayThis} failed: {callStatus}", 
                    callPhoneNumber, sayThis, call.Status);
            }

            failed.Guarantees("phone call failed.").IsFalse();
        }
    }

    public async Task SendToast(Guid userId, string subject, string details)
    {
        await _userToasts.AddToast(new UserToast
        {
            Id = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            UserId = userId,
            Subject = subject,
            Details = details
        });
        
    }
}
