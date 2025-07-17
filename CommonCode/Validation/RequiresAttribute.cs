using System;

namespace BFormDomain.Validation;

public enum Requires
{
   
    IsNull,
   
    IsNotNull,
 
    IsOfType,
  
    IsNotOfType,
   
    IsShorterThan,
  
    IsShorterOrEqual,
   
    IsLongerThan,
  
    IsLongerOrEqual,
  
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
   
    SupportsInterface
};


[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue,
    AllowMultiple = true, Inherited = true)]
public sealed class ValidateAttribute : Attribute
{


   
    public ValidateAttribute(Requires validationType)
    {
        this.ValidationType = validationType;
    }

   
    public Requires ValidationType { get; set; }

   
    public Type? TargetType { get; set; }

 
    public int TargetLength { get; set; }

  
    public object? Value { get; set; }

  
    public object? Max { get; set; }

  
    public const int GuidStringLength = 36;



}
