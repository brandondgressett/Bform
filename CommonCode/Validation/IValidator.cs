using System;

namespace BFormDomain.Validation
{

    public enum ExceptionType
    {
        
        None,
       
        IsNULL,
       
        IsNotNULL,
        
        IsOfType,
        
        IsNotOfType,
       
        IsShorter,
     
        IsNotShorter,
       
        IsShorterOrEqual,
       
        IsNotShorterOrEqual,
       
        IsLongerThan,
       
        IsNotLongerThan,
        
        IsLongerOrEqual,
       
        IsNotLongerOrEqual,
        
        HasLength,
        
        DoesNotHaveLength,
       
        IsNullOrEmpty,
        
        IsNotNullOrEmpty,
       
        IsEmpty,
        
        IsNotEmpty,
       
        StartsWith,
        
        DoesNotStartWith,
        
        Contains,
       
        DoesNotContain,
       
        ContainsAny,
       
        DoesNotContainAny,
       
        ContainsAll,
      
        DoesNotContainAll,
       
        EndsWith,
       
        DoesNotEndWith,
       
        IsInRange,
       
        IsNotInRange,
     
        IsGreaterThan,
      
        IsNotGreaterThan,
      
        IsGreaterOrEqual,
      
        IsNotGreaterOrEqual,
      
        IsLessThan,
      
        IsNotLessThan,
       
        IsLessOrEqual,
        
        IsNotLessOrEqual,
      
        IsEqualTo,
        
        IsNotEqualTo,
       
        IsTrue,
       
        IsFalse,
        
        Evaluate,
       
        SupportsInterface
    }

    
    public interface IValidator<T>
    {
        
        T? Value { get; }
        
        string? ArgumentName { get; }
       
        string? Message { get; }
       
        void Initialize(T? value, string? argumentName, string? message);

       
        IValidator<T> Otherwise<TException>(TException ex) where TException : Exception;
       
        Exception BuildValidationException(string? message, ExceptionType exceptionType);
    }

  
    [Serializable]
    public class ValidationException : Exception
    {
       
        public ValidationException() : base() { }
       
        public ValidationException(string? message) : base(message) { }
      
        public ValidationException(string? message, string condition)
            : base(message)
        {
            Condition = condition;
            UserMesage = message;
        }

      
        public ValidationException(string? message, Exception innerException) : base(message, innerException) { }

     
        public ValidationException(string? message, string condition, Exception innerException)
            : base(message, innerException)
        {
            Condition = condition;
            UserMesage = message;
        }

        public string? Condition { get; set; }
       
        public string? UserMesage { get; set; }

       
        public static void ThrowIf<T>(bool condition, string? message, IValidator<T> validator, ExceptionType exceptionType)
        {
            if (condition)
            {
                Exception ex = validator.BuildValidationException(message, exceptionType);
                throw ex;
            }
        }

    }
}
