using System;


namespace BFormDomain.Validation
{

    public class RequiresValidator<T> : ValidatorBase<T>
    {
       
        public RequiresValidator(T? value, string argumentName, string message) : base(value, argumentName, message) { }


      
        protected override Exception BuildException(string? message, ExceptionType exceptionType)
        {
            ArgumentException inner = new(message, argumentName);
            ValidationException ex = BuildSpecificException(message, inner, exceptionType);
            return ex;

        }
    }

}
