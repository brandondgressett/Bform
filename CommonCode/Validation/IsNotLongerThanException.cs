namespace BFormDomain.Validation
{
    [Serializable]

    public class IsNotLongerThanException : ValidationException
    {
       
        public IsNotLongerThanException() : base() { }
       
        public IsNotLongerThanException(string? message) : base(message) { }
       
        public IsNotLongerThanException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}