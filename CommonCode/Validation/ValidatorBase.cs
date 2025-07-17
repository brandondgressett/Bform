using System;


namespace BFormDomain.Validation
{


    public abstract class ValidatorBase<T> : IValidator<T>
    {
       
        public ValidatorBase()
        {
        }

      
        public ValidatorBase(T? value, string? argumentName, string? message)
        {
            Initialize(value, argumentName, message);
        }



      
        protected T? value;
     
        protected string? argumentName;
     
        protected string? message;
       
       
        protected abstract Exception BuildException(string? message, ExceptionType exceptionType);
      
        public T? Value
        {
            get { return value; }
        }

       
        public string? ArgumentName
        {
            get { return argumentName; }
        }

       
        public string? Message
        {
            get { return message; }
        }

      
        public void Initialize(T? value, string? argumentName, string? message)
        {
            this.value = value;
            this.argumentName = argumentName;
            this.message = message;
        }

       
        public IValidator<T> Otherwise<TException>(TException ex) where TException : Exception
        {
            return new OtherwiseValidator<T>(this, ex);
        }



       
        public Exception BuildValidationException(string? message, ExceptionType exceptionType)
        {
            return BuildException(message, exceptionType);
        }

     
        protected ValidationException BuildSpecificException(string? message, Exception innerException, ExceptionType exceptionType)
        {
            ValidationException? exception;

            if (exceptionType == ExceptionType.IsNULL)
                exception = new IsNullException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotNULL)
                exception = new IsNotNullException(message, innerException);
            else if (exceptionType == ExceptionType.IsOfType)
                exception = new IsOfTypeException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotOfType)
                exception = new IsNotOfTypeException(message, innerException);
            else if (exceptionType == ExceptionType.IsShorter)
                exception = new IsShorterException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotShorter)
                exception = new IsNotShorterException(message, innerException);
            else if (exceptionType == ExceptionType.IsShorterOrEqual)
                exception = new IsShorterOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotShorterOrEqual)
                exception = new IsNotShorterOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.IsLongerThan)
                exception = new IsLongerThanException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotLongerThan)
                exception = new IsNotLongerThanException(message, innerException);
            else if (exceptionType == ExceptionType.IsLongerOrEqual)
                exception = new IsLongerOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotLongerOrEqual)
                exception = new IsNotLongerOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.HasLength)
                exception = new HasLengthException(message, innerException);
            else if (exceptionType == ExceptionType.DoesNotHaveLength)
                exception = new DoesNotHaveLengthException(message, innerException);
            else if (exceptionType == ExceptionType.IsNullOrEmpty)
                exception = new IsNullOrEmptyException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotNullOrEmpty)
                exception = new IsNotNullOrEmptyException(message, innerException);
            else if (exceptionType == ExceptionType.IsEmpty)
                exception = new IsEmptyException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotEmpty)
                exception = new IsNotEmptyException(message, innerException);
            else if (exceptionType == ExceptionType.StartsWith)
                exception = new StartsWithException(message, innerException);
            else if (exceptionType == ExceptionType.DoesNotStartWith)
                exception = new DoesNotStartWithException(message, innerException);
            else if (exceptionType == ExceptionType.Contains)
                exception = new ContainsException(message, innerException);
            else if (exceptionType == ExceptionType.DoesNotContain)
                exception = new DoesNotContainException(message, innerException);
            else if (exceptionType == ExceptionType.ContainsAny)
                exception = new ContainsAnyException(message, innerException);
            else if (exceptionType == ExceptionType.DoesNotContainAny)
                exception = new DoesNotContainAnyException(message, innerException);
            else if (exceptionType == ExceptionType.ContainsAll)
                exception = new ContainsAllException(message, innerException);
            else if (exceptionType == ExceptionType.DoesNotContainAll)
                exception = new DoesNotContainAllException(message, innerException);
            else if (exceptionType == ExceptionType.EndsWith)
                exception = new EndsWithException(message, innerException);
            else if (exceptionType == ExceptionType.DoesNotEndWith)
                exception = new DoesNotEndWithException(message, innerException);
            else if (exceptionType == ExceptionType.IsInRange)
                exception = new IsInRangeException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotInRange)
                exception = new IsNotInRangeException(message, innerException);
            else if (exceptionType == ExceptionType.IsGreaterThan)
                exception = new IsGreaterThanException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotGreaterThan)
                exception = new IsNotGreaterThanException(message, innerException);
            else if (exceptionType == ExceptionType.IsGreaterOrEqual)
                exception = new IsGreaterOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotGreaterOrEqual)
                exception = new IsNotGreaterOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.IsLessThan)
                exception = new IsLessThanException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotLessThan)
                exception = new IsNotLessThanException(message, innerException);
            else if (exceptionType == ExceptionType.IsLessOrEqual)
                exception = new IsLessOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotLessOrEqual)
                exception = new IsNotLessOrEqualException(message, innerException);
            else if (exceptionType == ExceptionType.IsEqualTo)
                exception = new IsEqualToException(message, innerException);
            else if (exceptionType == ExceptionType.IsNotEqualTo)
                exception = new IsNotEqualToException(message, innerException);
            else if (exceptionType == ExceptionType.IsTrue)
                exception = new IsTrueException(message, innerException);
            else if (exceptionType == ExceptionType.IsFalse)
                exception = new IsFalseException(message, innerException);
            else if (exceptionType == ExceptionType.Evaluate)
                exception = new EvaluateException(message, innerException);
            else
                exception = new ValidationException(message, innerException);

            return exception;
        }

      
    }

  
    [Serializable]

    public class IsNullOrEmptyException : ValidationException
    {
       
        public IsNullOrEmptyException() : base() { }
       
        public IsNullOrEmptyException(string? message) : base(message) { }
    
        public IsNullOrEmptyException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    

    [Serializable]

    public class IsNotNullOrEmptyException : ValidationException
    {
        
        public IsNotNullOrEmptyException() : base() { }
        
        public IsNotNullOrEmptyException(string? message) : base(message) { }
        
        public IsNotNullOrEmptyException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class IsEmptyException : ValidationException
    {
      
        public IsEmptyException() : base() { }
       
        public IsEmptyException(string? message) : base(message) { }
       
        public IsEmptyException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

  

    [Serializable]

    public class IsNotEmptyException : ValidationException
    {
     
        public IsNotEmptyException() : base() { }
    
        public IsNotEmptyException(string? message) : base(message) { }
      
        public IsNotEmptyException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

 

    [Serializable]

    public class StartsWithException : ValidationException
    {
       
        public StartsWithException() : base() { }
    
        public StartsWithException(string? message) : base(message) { }
     
        public StartsWithException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class DoesNotStartWithException : ValidationException
    {
      
        public DoesNotStartWithException() : base() { }

        public DoesNotStartWithException(string? message) : base(message) { }
    
        public DoesNotStartWithException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class ContainsException : ValidationException
    {
       
        public ContainsException() : base() { }
       
        public ContainsException(string? message) : base(message) { }
      
        public ContainsException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    
    [Serializable]

    public class DoesNotContainException : ValidationException
    {
       
        public DoesNotContainException() : base() { }
        
        public DoesNotContainException(string? message) : base(message) { }
       
        public DoesNotContainException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class ContainsAnyException : ValidationException
    {
     
        public ContainsAnyException() : base() { }
       
        public ContainsAnyException(string? message) : base(message) { }
      
        public ContainsAnyException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class DoesNotContainAnyException : ValidationException
    {
       
        public DoesNotContainAnyException() : base() { }
       
        public DoesNotContainAnyException(string? message) : base(message) { }
      
        public DoesNotContainAnyException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   
    [Serializable]

    public class ContainsAllException : ValidationException
    {
      
        public ContainsAllException() : base() { }
       
        public ContainsAllException(string? message) : base(message) { }
       
        public ContainsAllException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class DoesNotContainAllException : ValidationException
    {
       
        public DoesNotContainAllException() : base() { }
       
        public DoesNotContainAllException(string? message) : base(message) { }
       
        public DoesNotContainAllException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class EndsWithException : ValidationException
    {
        
        public EndsWithException() : base() { }
       
        public EndsWithException(string? message) : base(message) { }
       
        public EndsWithException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    
    [Serializable]

    public class DoesNotEndWithException : ValidationException
    {
       
        public DoesNotEndWithException() : base() { }
      
        public DoesNotEndWithException(string? message) : base(message) { }
       
        public DoesNotEndWithException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class IsInRangeException : ValidationException
    {
       
        public IsInRangeException() : base() { }
      
        public IsInRangeException(string? message) : base(message) { }
      
        public IsInRangeException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class IsNotInRangeException : ValidationException
    {
       
        public IsNotInRangeException() : base() { }
       
        public IsNotInRangeException(string? message) : base(message) { }
       
        public IsNotInRangeException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class IsGreaterThanException : ValidationException
    {
       
        public IsGreaterThanException() : base() { }
       
        public IsGreaterThanException(string? message) : base(message) { }
        
        public IsGreaterThanException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    

    [Serializable]

    public class IsNotGreaterThanException : ValidationException
    {
       
        public IsNotGreaterThanException() : base() { }
     
        public IsNotGreaterThanException(string? message) : base(message) { }
      
        public IsNotGreaterThanException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

  

    [Serializable]

    public class IsGreaterOrEqualException : ValidationException
    {
       
        public IsGreaterOrEqualException() : base() { }
      
        public IsGreaterOrEqualException(string? message) : base(message) { }
       
        public IsGreaterOrEqualException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    

    [Serializable]

    public class IsNotGreaterOrEqualException : ValidationException
    {
       
        public IsNotGreaterOrEqualException() : base() { }
      
        public IsNotGreaterOrEqualException(string? message) : base(message) { }
       
        public IsNotGreaterOrEqualException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }


    [Serializable]

    public class IsLessThanException : ValidationException
    {
        
        public IsLessThanException() : base() { }
      
        public IsLessThanException(string? message) : base(message) { }
      
        public IsLessThanException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   
    [Serializable]

    public class IsNotLessThanException : ValidationException
    {
      
        public IsNotLessThanException() : base() { }
        
        public IsNotLessThanException(string? message) : base(message) { }
      
        public IsNotLessThanException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    

    [Serializable]

    public class IsLessOrEqualException : ValidationException
    {
      
        public IsLessOrEqualException() : base() { }
      
        public IsLessOrEqualException(string? message) : base(message) { }
        
        public IsLessOrEqualException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    

    [Serializable]

    public class IsNotLessOrEqualException : ValidationException
    {
     
        public IsNotLessOrEqualException() : base() { }
      
        public IsNotLessOrEqualException(string? message) : base(message) { }
       
        public IsNotLessOrEqualException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    

    [Serializable]

    public class IsEqualToException : ValidationException
    {
      
        public IsEqualToException() : base() { }
       
        public IsEqualToException(string? message) : base(message) { }
        
        public IsEqualToException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class IsNotEqualToException : ValidationException
    {
        
        public IsNotEqualToException() : base() { }
       
        public IsNotEqualToException(string? message) : base(message) { }
      
        public IsNotEqualToException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class IsTrueException : ValidationException
    {
        
        public IsTrueException() : base() { }
       
        public IsTrueException(string? message) : base(message) { }
        
        public IsTrueException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

   

    [Serializable]

    public class IsFalseException : ValidationException
    {
       
        public IsFalseException() : base() { }
        
        public IsFalseException(string? message) : base(message) { }
       
        public IsFalseException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    

    [Serializable]

    public class EvaluateException : ValidationException
    {
        
        public EvaluateException() : base() { }
       
        public EvaluateException(string? message) : base(message) { }
       
        public EvaluateException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}