namespace BFormDomain.CommonCode.Authorization;

public class InvitationLogicOptions
{
    public int DaysToExpire { get; set; } = 14;
    public string SubjectKey { get; set; } = "InvitationSubject";
    public string BodyKey { get; set; } = "InvitationBody";
}
