namespace BFormDomain.Validation
{
    [Serializable]
    public class IsNullException : ValidationException
    {
       
        public IsNullException() : base() { }
      
        public IsNullException(string? message) : base(message) { }
       
        public IsNullException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}