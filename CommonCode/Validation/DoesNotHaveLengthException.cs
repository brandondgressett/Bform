namespace BFormDomain.Validation
{
    [Serializable]

    public class DoesNotHaveLengthException : ValidationException
    {
        
        public DoesNotHaveLengthException() : base() { }
       
        public DoesNotHaveLengthException(string? message) : base(message) { }
       
        public DoesNotHaveLengthException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}