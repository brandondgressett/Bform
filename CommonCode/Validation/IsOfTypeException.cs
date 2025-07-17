namespace BFormDomain.Validation
{
    [Serializable]
    public class IsOfTypeException : ValidationException
    {
       
        public IsOfTypeException() : base() { }
       
        public IsOfTypeException(string? message) : base(message) { }
       
        public IsOfTypeException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}