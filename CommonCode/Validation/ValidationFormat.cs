using System;
using System.Collections;
using System.Text;


namespace BFormDomain.Validation
{
    
    public static class ValidationFormat
    {
        

        public static void InternalStringValidate(ValidateAttribute att, string param, string name)
        {
            string? value = att.Value as string;
            

            int length = att.TargetLength;

            switch (att.ValidationType)
            {
                case Requires.Contains:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must contain \"{1}\".", name, value)).Contains(value!);

                    }
                    break;

                case Requires.ContainsAll:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must contain all characters in \"{1}\".", name, value)).ContainsAll(value!);

                    }
                    break;

                case Requires.ContainsAny:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must contain any characters in \"{1}\".", name, value)).ContainsAny(value!);

                    }
                    break;

                case Requires.DoesNotContain:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} does not contain \"{1}\"", name, value)).DoesNotContain(value!);
                    }
                    break;

                case Requires.DoesNotContainAll:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} does not contain all characters in \"{1}\".", name, value)).DoesNotContainAll(value!);
                    }
                    break;

                case Requires.DoesNotContainAny:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} does not contain any characters in \"{1}\".", name, value)).DoesNotContainAny(value!);
                    }
                    break;

                case Requires.DoesNotEndWith:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not end with \"{1}\".", name, value)).DoesNotEndWith(value!);
                    }
                    break;

                case Requires.DoesNotHaveLength:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not have length {1}.", name, length)).DoesNotHaveLength(length);
                    }
                    break;

                case Requires.DoesNotStartWith:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} does not start with \"{1}\".", name, value)).DoesNotStartWith(value!);
                    }
                    break;

                case Requires.EndsWith:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must end with \"{1}\".", name, value)).EndsWith(value!);
                    }
                    break;

                case Requires.HasLength:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must have length {1}", name, length)).HasLength(length);
                    }
                    break;

                case Requires.IsEmpty:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be empty.", name)).IsEmpty();
                    }
                    break;

                case Requires.IsEqualTo:
                case Requires.IsGreaterOrEqual:
                case Requires.IsGreaterThan:
                case Requires.IsLessOrEqual:
                case Requires.IsLessThan:
                case Requires.IsNotEqualTo:
                case Requires.IsNotGreaterOrEqual:
                case Requires.IsNotGreaterThan:
                case Requires.IsNotLessOrEqual:
                case Requires.IsNotLessThan:
                    {
                        IComparable pComp = param as IComparable;
                        InternalComparableValidate(att, pComp, name);
                    }
                    break;



                case Requires.IsLongerOrEqual:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} length must be longer or equal to {1}.", name, length)).IsLongerOrEqual(length);
                    }
                    break;


                case Requires.IsLongerThan:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} length must longer than {1}.", name, length)).IsLongerThan(length);
                    }
                    break;

                case Requires.IsNotEmpty:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not be empty.", name)).IsNotEmpty();
                    }
                    break;


                case Requires.IsNotNullOrEmpty:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not be null or empty.", name)).IsNotNullOrEmpty();
                    }
                    break;

                case Requires.IsNullOrEmpty:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be null or empty.", name)).IsNullOrEmpty();
                    }
                    break;


                case Requires.IsShorterOrEqual:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} length must be equal to or shorter than {1}.", name, length)).IsShorterOrEqual(length);
                    }
                    break;

                case Requires.IsShorterThan:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} length must be shorter than {1}.", name, length)).IsShorterThan(length);
                    }
                    break;



                case Requires.StartsWith:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must start with \"{1}\".", name, value)).StartsWith(value!);
                    }
                    break;
            }
        }

        public static string FormatEnumerator(IEnumerable values)
        {
            StringBuilder builder = new();
            builder.Append('{');
            const int enough = 7;

            int itemCount = 0;


            foreach (object value in values)
            {
                builder.Append(value.ToString());

                itemCount++;
                if (itemCount > enough)
                {
                    builder.AppendFormat(" ... ");
                    break;
                }

                builder.Append(", ");
            }

            builder.Append('}');
            return builder.ToString();
        }

        public static void InternalEnumValidate(ValidateAttribute att, IEnumerable param, string name)
        {
            if (att.Value is not IEnumerable values) throw new ArgumentNullException(nameof(att));

            int length = att.TargetLength;
            object? value = att.Value;

            switch (att.ValidationType)
            {
                case Requires.Contains:
                    {
                        param.Requires(name, $"{name} must contain value {value}").Contains(value!);

                    }
                    break;

                case Requires.ContainsAll:
                    {
                        param.Requires(name, $"{name} must contain all of {FormatEnumerator(values!)}").ContainsAll(values);

                    }
                    break;

                case Requires.ContainsAny:
                    {

                        param.Requires(name, $"{name} must contain any of {FormatEnumerator(values!)}").ContainsAny(values);

                    }
                    break;

                case Requires.DoesNotContain:
                    {
                        param.Requires(name, $"{name} must not contain {value}").DoesNotContain(value!);
                    }
                    break;

                case Requires.DoesNotContainAll:
                    {
                        param.Requires(name, $"{name} must not contain all of {FormatEnumerator(values!)}").DoesNotContainAll(values);
                    }
                    break;

                case Requires.DoesNotContainAny:
                    {
                        param.Requires(name, $"{name} must not contain any of {FormatEnumerator(values!)}").DoesNotContainAny(values);
                    }
                    break;


                case Requires.DoesNotHaveLength:
                    {
                        param.Requires(name, $"{name} must not have lengeth {length}").DoesNotHaveLength(length);
                    }
                    break;



                case Requires.HasLength:
                    {
                        param.Requires(name, $"{name} must have length {length}").HasLength(length);
                    }
                    break;

                case Requires.IsEmpty:
                    {
                        param.Requires(name, $"{name} must be empty").IsEmpty();
                    }
                    break;



                case Requires.IsNotEmpty:
                    {
                        param.Requires(name, $"{name} must not be empty").IsNotEmpty();
                    }
                    break;



                case Requires.IsShorterOrEqual:
                    {
                        param.Requires(name, $"{name} must be shorter than or equal to length of {length}").IsShorterOrEqual(length);
                    }
                    break;

                case Requires.IsShorterThan:
                    {
                        param.Requires(name, $"{name} must be short than length {length}").IsShorterThan(length);
                    }
                    break;

            }
        }

        public static void InternalComparableValidate(ValidateAttribute att, IComparable param, string name)
        {
            IComparable? value = att.Value as IComparable;
            IComparable? max = att.Max as IComparable;

            switch (att.ValidationType)
            {

                case Requires.IsEqualTo:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be equal to {1}.", name, att.Value!.ToString())).IsEqualTo(value!);
                    }
                    break;


                case Requires.IsGreaterOrEqual:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be greater than {1}.", name, att.Value!.ToString())).IsGreaterOrEqual(value!);
                    }
                    break;

                case Requires.IsGreaterThan:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be greater than {1}.", name, att.Value!.ToString())).IsGreaterThan(value!);
                    }
                    break;

                case Requires.IsInRange:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be within range {1} |TO| {2}", name, att.Value!.ToString(), att.Max!.ToString())).IsInRange(value!, max!);
                    }
                    break;

                case Requires.IsLessOrEqual:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be less than or equal than {1}.", name, att.Value!.ToString())).IsLessOrEqual(value!);
                    }
                    break;

                case Requires.IsLessThan:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be less than {1}.", name, att.Value!.ToString())).IsLessThan(value!);
                    }
                    break;

                case Requires.IsNotEqualTo:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must be equal to {1}.", name, att.Value!.ToString())).IsNotEqualTo(value!);
                    }
                    break;

                case Requires.IsNotGreaterOrEqual:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not be greater or equal to {1}.", name, att.Value!.ToString())).IsNotGreaterOrEqual(value!);
                    }
                    break;

                case Requires.IsNotGreaterThan:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not be greater than {1}.", name, att.Value!.ToString())).IsNotGreaterThan(value!);
                    }
                    break;

                case Requires.IsNotInRange:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not be in range {1} |TO| {2}.", name, att.Value!.ToString(), att.Max!.ToString())).IsNotInRange(value!, max!);
                    }
                    break;

                case Requires.IsNotLessOrEqual:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not be less than or equal to {1}.", name, att.Value!.ToString())).IsNotLessOrEqual(value!);
                    }
                    break;

                case Requires.IsNotLessThan:
                    {
                        param.Requires(name, String.Format("Validation enforces {0} must not be less than {1}.", name, att.Value!.ToString())).IsNotLessThan(value!);
                    }
                    break;

            }
        }
    }
}