namespace BFormDomain.Validation
{
    [Serializable]
    public class IsNotShorterException : ValidationException
    {
       
        public IsNotShorterException() : base() { }
      
        public IsNotShorterException(string? message) : base(message) { }
      
        public IsNotShorterException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}