namespace BFormDomain.Validation
{
    [Serializable]
    public class IsNotShorterOrEqualException : ValidationException
    {
      
        public IsNotShorterOrEqualException() : base() { }
        
        public IsNotShorterOrEqualException(string? message) : base(message) { }
        
        public IsNotShorterOrEqualException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}