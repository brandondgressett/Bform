namespace BFormDomain.Validation
{
    [Serializable]
    public class IsNotNullException : ValidationException
    {
        
        public IsNotNullException() : base() { }
      
        public IsNotNullException(string? message) : base(message) { }
       
        public IsNotNullException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}