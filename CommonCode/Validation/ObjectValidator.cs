using System;
using System.Collections;
using System.Linq;
using System.Reflection;


namespace BFormDomain.Validation
{
    
    public static class ObjectValidator
    {
        public static void Validate(object obj)
        {
            var pi = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in pi)
            {
                if (prop.CanRead)
                {
                    var val = prop.GetValue(obj);
                    if (val is not null)
                    {
                        var validations = prop.GetCustomAttributes(typeof(ValidateAttribute), true);
                        ValidateProperty(validations, val, prop);
                    }
                    


                }

            }
        }
        

        private static void ValidateProperty(object[] validations, object propVal, PropertyInfo info)
        {
            if (validations != null)
                foreach (object v in validations)
                {
                    if (v is null)
                        continue;

                    Type? pType;
                    IComparable? pComp = null!;
                    IEnumerable? pEnum = null!;
                    string? stringVal = null!;

                    if (propVal is not null)
                    {
                        pType = propVal.GetType();
                        pComp = propVal as IComparable;
                        pEnum = propVal as IEnumerable;
                        stringVal = (pType == typeof(string)) ? (propVal as string) : null;
                    }

                    if (v is not ValidateAttribute att)
                        continue;

                    switch (att.ValidationType)
                    {
                        case Requires.IsTrue:
                            {
                                propVal!.Requires().IsOfType(typeof(bool));
                                bool boolVal = (bool)propVal!;
                                boolVal.Requires(info.Name, $"{info.Name} must be true")
                                       .IsTrue();
                            }
                            break;

                        case Requires.IsFalse:
                            {
                                propVal!.Requires().IsOfType(typeof(bool));
                                bool boolVal = (bool)propVal!;
                                boolVal.Requires(info.Name,$"{info.Name} must be false")
                                       .IsFalse();
                            }
                            break;

                        case Requires.IsOfType:
                            {
                                propVal!.Requires(info.Name, $"{info.Name} must of type {att.TargetType!.Name}").IsOfType(att.TargetType);
                            }
                            break;

                        case Requires.IsNotOfType:
                            {
                                propVal!.Requires(info.Name, $"{info.Name} must not be of type {att.TargetType!.Name}").IsNotOfType(att.TargetType);
                            }
                            break;

                        case Requires.SupportsInterface:
                            {
                                propVal!.Requires(info.Name, $"{info.Name} must support interface {att.TargetType!.Name}").SupportsInterface(att.TargetType);
                            }
                            break;

                        case Requires.IsNull:
                            {
                                propVal!.Requires(info.Name, $"{info.Name} must be null").IsNull();
                            }
                            break;

                        case Requires.IsNotNull:
                            {
                                propVal!.Requires(info.Name, $"{info.Name} must not be null")
                                        .IsNotNull();
                            }
                            break;


                        default:
                            {
                                if (stringVal != null)
                                    ValidationFormat.InternalStringValidate(att, stringVal, info.Name);
                                else if (pEnum != null)
                                    ValidationFormat.InternalEnumValidate(att, pEnum, info.Name);
                                else if (pComp != null)
                                    ValidationFormat.InternalComparableValidate(att, pComp, info.Name);
                            }
                            break;
                    }
                }
        }
    }
}
