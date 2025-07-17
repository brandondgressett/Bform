namespace BFormDomain.Validation
{
    [Serializable]

    public class IsShorterOrEqualException : ValidationException
    {
       
        public IsShorterOrEqualException() : base() { }
       
        public IsShorterOrEqualException(string? message) : base(message) { }
      
        public IsShorterOrEqualException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}