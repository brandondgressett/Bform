namespace BFormDomain.Validation
{
    [Serializable]

    public class IsShorterException : ValidationException
    {
        public IsShorterException() : base() { }
       
        public IsShorterException(string? message) : base(message) { }
       
        public IsShorterException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}