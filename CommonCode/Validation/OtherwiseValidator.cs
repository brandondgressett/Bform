using System;


namespace BFormDomain.Validation
{
 
    public class OtherwiseValidator<T> : ValidatorBase<T>
    {
     
        readonly Exception ex;
        

       
        public OtherwiseValidator(IValidator<T> validator, Exception exception)
            : base(validator.Value, validator.ArgumentName, validator.Message)
        {

            ex = exception;
        }


     
        protected override Exception BuildException(string? message, ExceptionType exceptionType)
        {
            return ex;
        }
    }
}
