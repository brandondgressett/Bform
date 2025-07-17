using System;


namespace BFormDomain.Validation;

public class GuaranteesValidator<T> : ValidatorBase<T>
{

    
    public GuaranteesValidator(T? value, string? argumentName, string? message) : base(value, argumentName, message) { }


    protected override Exception BuildException(string? message, ExceptionType exceptionType)
    {
        InvalidOperationException inner = new(argumentName);
        ValidationException ex = BuildSpecificException(message, inner, exceptionType);
        return ex;
    }
}
