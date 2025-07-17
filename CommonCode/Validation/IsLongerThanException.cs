namespace BFormDomain.Validation
{
    [Serializable]

    public class IsLongerThanException : ValidationException
    {
        public IsLongerThanException() : base() { }
       
        public IsLongerThanException(string? message) : base(message) { }
        
        public IsLongerThanException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}