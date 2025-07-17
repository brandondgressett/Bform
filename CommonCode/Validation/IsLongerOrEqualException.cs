namespace BFormDomain.Validation;

[Serializable]

public class IsLongerOrEqualException : ValidationException
{
    
    public IsLongerOrEqualException() : base() { }
    
    public IsLongerOrEqualException(string? message) : base(message) { }
   
    public IsLongerOrEqualException(string? message, Exception innerException)
        : base(message, innerException)
    {
    }
}
