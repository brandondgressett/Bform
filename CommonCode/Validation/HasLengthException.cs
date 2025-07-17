namespace BFormDomain.Validation;

[Serializable]

public class HasLengthException : ValidationException
{
   
    public HasLengthException() : base() { }
  
    public HasLengthException(string? message) : base(message) { }
    
    public HasLengthException(string? message, Exception innerException)
        : base(message, innerException)
    {
    }
}
