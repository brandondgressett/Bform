namespace BFormDomain.Validation
{
    [Serializable]

    public class IsNotOfTypeException : ValidationException
    {
       
        public IsNotOfTypeException() : base() { }
       
        public IsNotOfTypeException(string? message) : base(message) { }
      
        public IsNotOfTypeException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}