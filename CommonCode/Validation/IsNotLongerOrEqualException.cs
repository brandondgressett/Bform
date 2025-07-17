namespace BFormDomain.Validation
{
    [Serializable]

    public class IsNotLongerOrEqualException : ValidationException
    {
       
        public IsNotLongerOrEqualException() : base() { }
     
        public IsNotLongerOrEqualException(string? message) : base(message) { }
       
        public IsNotLongerOrEqualException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}