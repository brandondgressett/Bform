namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPIInsufficientDataException: ApplicationException
{
    public KPIInsufficientDataException()
    {

    }

    public KPIInsufficientDataException(string? message): base(message)
    {

    }

    public KPIInsufficientDataException(string? message, Exception innerException): 
        base(message, innerException)
    {

    }

}
