using System.Collections;
using System.Linq.Expressions;


namespace BFormDomain.Validation;

public static partial class ValidatorExtensions
{
    #region Requires


    /// <summary>
    /// Requireses the specified value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<T> Requires<T>(this T? value)
    {
        return new RequiresValidator<T>(value, "value", $"requirement failed for {value}");
    }

    /// <summary>
    /// Requires the specified value with error message if validation fails.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <param name="message">The message.</param>
    /// <returns></returns>
    public static IValidator<T> Requires<T>(this T value, string message)
    {
        return new RequiresValidator<T>(value, "value", message);
    }

    /// <summary>
    /// Requireses the specified value with the argument name and error message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <param name="argumentName">Name of the argument.</param>
    /// <param name="message">The message.</param>
    /// <returns></returns>
    public static IValidator<T> Requires<T>(this T value, string argumentName, string message)
    {
        return new RequiresValidator<T>(value, argumentName, message);
    }
    #endregion

    #region Guarantees
    /// <summary>
    /// Guaranteeses the specified value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<T> Guarantees<T>(this T? value)
    {
        return new GuaranteesValidator<T>(value, "value", $"guarantee failed for value {value}");
    }

    /// <summary>
    /// Guaranteeses the specified value and error message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <param name="message">The message.</param>
    /// <returns></returns>
    public static IValidator<T> Guarantees<T>(this T? value, string? message)
    {
        return new GuaranteesValidator<T>(value!, "value", message);
    }

    /// <summary>
    /// Guaranteeses the specified value, argument name and error message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <param name="argumentName">Name of the argument.</param>
    /// <param name="message">The message.</param>
    /// <returns></returns>
    public static IValidator<T> Guarantees<T>(this T? value, string? argumentName, string? message)
    {
        return new GuaranteesValidator<T>(value!, argumentName, message);
    }

    #endregion

    #region Evaluation
    /// <summary>
    /// Evaluates the specified validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="expression">The expression.</param>
    /// <param name="expressionDescription">The expression description.</param>
    /// <returns></returns>
    public static IValidator<T> Evaluate<T>(
        this IValidator<T> validator,
        Expression<Func<T?, bool>> expression,
        string expressionDescription)
    {
        bool valueIsValid = false;

        if (validator is not null)
        {
            if (expression is not null)
            {

                Func<T?, bool> func = expression.Compile();

                valueIsValid = func(validator.Value);

            }


            ValidationException.ThrowIf(valueIsValid, expressionDescription, validator, ExceptionType.Evaluate);
        }

        return validator!;
    }

    /// <summary>
    /// Evaluates the specified validator, condition and condition description.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="condition">if set to <c>true</c> [condition].</param>
    /// <param name="conditionDescription">The condition description.</param>
    /// <returns></returns>
    public static IValidator<T> Evaluate<T>(
        this IValidator<T> validator,
        bool condition,
        string conditionDescription)
    {
        ValidationException.ThrowIf(condition, conditionDescription, validator, ExceptionType.Evaluate);

        return validator;
    }
    #endregion

    #region IsNull
    /// <summary>
    /// Determines whether the specified validator is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<T> IsNull<T>(this IValidator<T> validator)
        where T : class
    {
        ValidationException.ThrowIf(validator.Value is not null, validator.Message, validator, ExceptionType.IsNotNULL);
        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNull<T>(this IValidator<Nullable<T>> validator)
        where T : struct
    {
        ValidationException.ThrowIf(validator.Value.HasValue, validator.Message, validator, ExceptionType.IsNotNULL);
        return validator;
    }

    #endregion

    #region IsNotNull
    /// <summary>
    /// Determines whether the specified validator is not null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotNull<T>(this IValidator<T> validator) where T : class
    {
        ValidationException.ThrowIf(validator.Value is null, validator.Message, validator, ExceptionType.IsNULL);
        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotNull<T>(this IValidator<Nullable<T>> validator)
        where T : struct
    {
        ValidationException.ThrowIf(!validator.Value.HasValue, validator.Message, validator, ExceptionType.IsNULL);
        return validator;
    }
    #endregion

    #region Type
    /// <summary>
    /// Determines whether the specified validator is of type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    public static IValidator<T> IsOfType<T>(this IValidator<T> validator, Type type)
        where T : class
    {

        T? value = validator.Value;

        if (value is not null)
        {
            bool valueIsValid = type.IsAssignableFrom(value.GetType());
            ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotOfType);
        }

        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not of type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotOfType<T>(this IValidator<T> validator, Type type)
        where T : class
    {

        T? value = validator.Value;

        if (value is not null)
        {
            bool valueIsInvalid = type.IsAssignableFrom(value.GetType());
            ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsOfType);
        }

        return validator;
    }

    /// <summary>
    /// Determinses whether the sepecifed validator supportses the interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="interfaceType">Type of the interface.</param>
    /// <returns></returns>
    public static IValidator<T> SupportsInterface<T>(this IValidator<T> validator, Type interfaceType)
        where T : class
    {
        T? value = validator.Value;

        if (value is not null)
        {
            Type testType = value.GetType();
            ValidationException.ThrowIf(testType.GetInterface(interfaceType.Name, false) == null, validator.Message, validator, ExceptionType.SupportsInterface);

        }

        return validator;
    }


    #endregion

    #region String
    /// <summary>
    /// Determines whether the specified validator is shorter than the specfied length.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxLength">Length of the max.</param>
    /// <returns></returns>
    public static IValidator<string> IsShorterThan(this IValidator<string> validator, int maxLength)
    {
        string? value = validator.Value;

        int valueLength = 0;
        if (value is not null)
        {
            valueLength = value.Length;
        }

        ValidationException.ThrowIf(!(valueLength < maxLength), validator.Message, validator, ExceptionType.IsNotShorter);


        return validator;
    }


    /// <summary>
    /// Determines whether the specified validator string is shorter or equal the given length.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxLength">Length of the max.</param>
    /// <returns></returns>
    public static IValidator<string> IsShorterOrEqual(this IValidator<string> validator, int maxLength)
    {
        string? value = validator.Value;

        int valueLength = 0;
        if (value is not null)
        {
            valueLength = value.Length;
        }

        ValidationException.ThrowIf(!(valueLength <= maxLength), validator.Message, validator, ExceptionType.IsNotShorterOrEqual);

        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string is longer than the given minimal length.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minLength">Length of the min.</param>
    /// <returns></returns>
    public static IValidator<string> IsLongerThan(this IValidator<string> validator, int minLength)
    {
        string? value = validator.Value;

        int valueLength = 0;
        if (value is not null)
        {
            valueLength = value.Length;
        }

        ValidationException.ThrowIf(!(valueLength > minLength), validator.Message, validator, ExceptionType.IsNotLongerThan);


        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string is longer than or equal to the minimal length.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minLength">Length of the min.</param>
    /// <returns></returns>
    public static IValidator<string> IsLongerOrEqual(this IValidator<string> validator, int minLength)
    {
        string? value = validator.Value;

        int valueLength = 0;
        if (value is not null)
        {
            valueLength = value.Length;
        }

        ValidationException.ThrowIf(!(valueLength >= minLength), validator.Message, validator, ExceptionType.IsNotLongerOrEqual);


        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string length equals to the given length.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="length">The length.</param>
    /// <returns></returns>
    public static IValidator<string> HasLength(this IValidator<string> validator, int length)
    {
        string? value = validator.Value;

        int valueLength = 0;
        if (value is not null)
        {
            valueLength = value.Length;
        }

        ValidationException.ThrowIf(!(valueLength == length), validator.Message, validator, ExceptionType.DoesNotHaveLength);
        return validator;
    }

    /// <summary>
    /// Determings whether the specified validator string length not equals to the given length.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="length">The length.</param>
    /// <returns></returns>
    public static IValidator<string> DoesNotHaveLength(this IValidator<string> validator, int length)
    {
        string? value = validator.Value;

        int valueLength = 0;
        if (value is not null)
        {
            valueLength = value.Length;
        }

        ValidationException.ThrowIf(!(valueLength != length), validator.Message, validator, ExceptionType.HasLength);

        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string is null or empty.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<string> IsNullOrEmpty(this IValidator<string> validator)
    {
        ValidationException.ThrowIf(!String.IsNullOrEmpty(validator.Value), validator.Message, validator, ExceptionType.IsNotNullOrEmpty);
        return validator;
    }


    /// <summary>
    /// Determines whether the specified validator string is not null or empty.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<string> IsNotNullOrEmpty(this IValidator<string> validator)
    {
        bool valueIsInvalid = String.IsNullOrEmpty(validator.Value);
        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsNullOrEmpty);
        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string is empty.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<string> IsEmpty(this IValidator<string> validator)
    {
        ValidationException.ThrowIf(!(validator.Value != null && validator.Value.Length == 0), validator.Message, validator, ExceptionType.IsEmpty);
        return validator;
    }


    /// <summary>
    /// Determines whether the specified validator string is not empty.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<string> IsNotEmpty(this IValidator<string> validator)
    {
        ValidationException.ThrowIf(validator.Value != null && validator.Value.Length == 0, validator.Message, validator, ExceptionType.IsNotEmpty);
        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string startses with the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<string> StartsWith(this IValidator<string> validator, string? value)
    {
        return validator.StartsWith(value, StringComparison.CurrentCulture);
    }

    /// <summary>
    /// Determines whether the specified validator string startses with the input value by using given comparison.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <param name="comparisonType">Type of the comparison.</param>
    /// <returns></returns>
    public static IValidator<string> StartsWith(this IValidator<string> validator, string? value,
        StringComparison comparisonType)
    {
        string? validatorValue = validator.Value;

        bool valueIsValid =
            (value is null && validatorValue is null) ||
            (value is not null && validatorValue is not null && validatorValue.StartsWith(value, comparisonType));

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.DoesNotStartWith);
        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string does not startse with the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<string> DoesNotStartWith(this IValidator<string> validator, string? value)
    {
        return validator.DoesNotStartWith(value, StringComparison.CurrentCulture);
    }

    /// <summary>
    /// Determines whether the specified validator string does not startse with the given value by using given comparsion.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <param name="comparisonType">Type of the comparison.</param>
    /// <returns></returns>
    public static IValidator<string> DoesNotStartWith(this IValidator<string> validator, string? value,
        StringComparison comparisonType)
    {
        string? validatorValue = validator.Value;

        bool valueIsInvalid =
            (value is null && validatorValue is null) ||
            (value is not null && validatorValue is not null && validatorValue.StartsWith(value, comparisonType));

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.StartsWith);

        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string contains the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<string> Contains(this IValidator<string> validator, string? value)
    {
        string? validatorValue = validator.Value;

        bool valueIsValid =
            (value is null && validatorValue is null) ||
            (value is not null && validatorValue is not null && validatorValue.Contains(value));

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.DoesNotContain);

        return validator;
    }

    /// <summary>
    ///  Determines whether the specified validator string does not contain the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<string> DoesNotContain(this IValidator<string> validator, string? value)
    {
        string? validatorValue = validator.Value;

        bool valueIsInvalid =
            (value is null && validatorValue is null) ||
            (value is not null && validatorValue is not null && validatorValue.Contains(value));

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.Contains);

        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string ends with the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<string> EndsWith(this IValidator<string> validator, string? value)
    {
        return validator.EndsWith(value, StringComparison.CurrentCulture);
    }

    /// <summary>
    /// Determines whether the specified validator string ends with the given value by using specified comparison.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <param name="comparisonType">Type of the comparison.</param>
    /// <returns></returns>
    public static IValidator<string> EndsWith(this IValidator<string> validator, string? value,
        StringComparison comparisonType)
    {
        string? validatorValue = validator.Value;

        bool valueIsValid =
            (value is null && validatorValue is null) ||
            (value is not null && validatorValue is not null && validatorValue.EndsWith(value, comparisonType));

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.DoesNotEndWith);

        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator string does not end with the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<string> DoesNotEndWith(this IValidator<string> validator, string? value)
    {
        return validator.DoesNotEndWith(value, StringComparison.CurrentCulture);
    }

    /// <summary>
    /// Determines whether the specified validator string does not end with the given value by using specified comparison.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <param name="comparisonType">Type of the comparison.</param>
    /// <returns></returns>
    public static IValidator<string> DoesNotEndWith(this IValidator<string> validator, string? value,
         StringComparison comparisonType)
    {
        string? validatorValue = validator.Value;

        bool valueIsInvalid =
            (value is null && validatorValue is null) ||
            (value is not null && validatorValue is not null && validatorValue.EndsWith(value, comparisonType));

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.EndsWith);

        return validator;
    }
    #endregion

    #region IComparable
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<T> IsInRange<T>(this IValidator<T> validator, T minValue, T maxValue)
        where T : IComparable
    {
        Comparer<T> defaultComparer = Comparer<T>.Default;

        T? value = validator.Value;

        bool valueIsValid =
            defaultComparer.Compare(value, minValue) >= 0 &&
            defaultComparer.Compare(value, maxValue) <= 0;

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }


    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsInRange<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> minValue, Nullable<T> maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> defaultComparer = Comparer<Nullable<T>>.Default;

        Nullable<T> value = validator.Value;

        bool valueIsValid =
            defaultComparer.Compare(value, minValue) >= 0 &&
            defaultComparer.Compare(value, maxValue) <= 0;

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }


    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsInRange<T>(this IValidator<Nullable<T>> validator,
        T minValue, T maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> defaultComparer = Comparer<Nullable<T>>.Default;

        Nullable<T> value = validator.Value;

        bool valueIsValid =
            defaultComparer.Compare(value, minValue) >= 0 &&
            defaultComparer.Compare(value, maxValue) <= 0;

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }


    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotInRange<T>(this IValidator<T> validator, T minValue, T maxValue)
        where T : IComparable
    {
        Comparer<T> defaultComparer = Comparer<T>.Default;

        T? value = validator.Value;

        bool valueIsInvalid =
            defaultComparer.Compare(value, minValue) >= 0 &&
            defaultComparer.Compare(value, maxValue) <= 0;

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }


    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotInRange<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> minValue, Nullable<T> maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> defaultComparer = Comparer<Nullable<T>>.Default;

        Nullable<T> value = validator.Value;

        bool valueIsInvalid =
            defaultComparer.Compare(value, minValue) >= 0 &&
            defaultComparer.Compare(value, maxValue) <= 0;

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotInRange<T>(this IValidator<Nullable<T>> validator,
        T minValue, T maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> defaultComparer = Comparer<Nullable<T>>.Default;

        Nullable<T> value = validator.Value;

        bool valueIsInvalid =
            defaultComparer.Compare(value, minValue) >= 0 &&
            defaultComparer.Compare(value, maxValue) <= 0;

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<T> IsGreaterThan<T>(this IValidator<T> validator, T minValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(!(Comparer<T>.Default.Compare(validator.Value, minValue) > 0), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsGreaterThan<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> minValue)
        where T : struct
    {
        ValidationException.ThrowIf(!(Comparer<Nullable<T>>.Default.Compare(validator.Value, minValue) > 0), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsGreaterThan<T>(this IValidator<Nullable<T>> validator,
        T minValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsValid = (comparer.Compare(validator.Value, minValue) > 0);

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotGreaterThan<T>(this IValidator<T> validator, T maxValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(Comparer<T>.Default.Compare(validator.Value, maxValue) > 0, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotGreaterThan<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> maxValue)
        where T : struct
    {
        ValidationException.ThrowIf(Comparer<Nullable<T>>.Default.Compare(validator.Value, maxValue) > 0, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotGreaterThan<T>(this IValidator<Nullable<T>> validator,
        T maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsInvalid = (comparer.Compare(validator.Value, maxValue) > 0);

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<T> IsGreaterOrEqual<T>(this IValidator<T> validator, T minValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(!(Comparer<T>.Default.Compare(validator.Value, minValue) >= 0), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsGreaterOrEqual<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> minValue)
        where T : struct
    {
        ValidationException.ThrowIf(!(Comparer<Nullable<T>>.Default.Compare(validator.Value, minValue) >= 0), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsGreaterOrEqual<T>(this IValidator<Nullable<T>> validator,
        T minValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsValid = (comparer.Compare(validator.Value, minValue) >= 0);

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotGreaterOrEqual<T>(this IValidator<T> validator, T maxValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(Comparer<T>.Default.Compare(validator.Value, maxValue) >= 0, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotGreaterOrEqual<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> maxValue)
        where T : struct
    {
        ValidationException.ThrowIf(Comparer<Nullable<T>>.Default.Compare(validator.Value, maxValue) >= 0, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotGreaterOrEqual<T>(this IValidator<Nullable<T>> validator,
        T maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsInvalid = (comparer.Compare(validator.Value, maxValue) >= 0);

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<T> IsLessThan<T>(this IValidator<T> validator, T maxValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(!(Comparer<T>.Default.Compare(validator.Value, maxValue) < 0), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsLessThan<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> maxValue)
        where T : struct
    {
        ValidationException.ThrowIf(!(Comparer<Nullable<T>>.Default.Compare(validator.Value, maxValue) < 0), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsLessThan<T>(this IValidator<Nullable<T>> validator,
        T maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsValid = (comparer.Compare(validator.Value, maxValue) < 0);

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotLessThan<T>(this IValidator<T> validator, T minValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(Comparer<T>.Default.Compare(validator.Value, minValue) < 0, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotLessThan<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> minValue)
        where T : struct
    {
        ValidationException.ThrowIf(Comparer<Nullable<T>>.Default.Compare(validator.Value, minValue) < 0, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotLessThan<T>(this IValidator<Nullable<T>> validator,
        T minValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsInvalid = (comparer.Compare(validator.Value, minValue) < 0);

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<T> IsLessOrEqual<T>(this IValidator<T> validator, T maxValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(!(Comparer<T>.Default.Compare(validator.Value, maxValue) <= 0), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsLessOrEqual<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> maxValue)
        where T : struct
    {
        ValidationException.ThrowIf(!(Comparer<Nullable<T>>.Default.Compare(validator.Value, maxValue) <= 0), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsLessOrEqual<T>(this IValidator<Nullable<T>> validator,
        T maxValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsValid = (comparer.Compare(validator.Value, maxValue) <= 0);

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotLessOrEqual<T>(this IValidator<T> validator, T minValue)
        where T : IComparable
    {
        ValidationException.ThrowIf(Comparer<T>.Default.Compare(validator.Value, minValue) <= 0, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotLessOrEqual<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> minValue)
        where T : struct
    {
        ValidationException.ThrowIf(Comparer<Nullable<T>>.Default.Compare(validator.Value, minValue) <= 0, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotLessOrEqual<T>(this IValidator<Nullable<T>> validator,
        T minValue)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsInvalid = (comparer.Compare(validator.Value, minValue) <= 0);

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<T> IsEqualTo<T>(this IValidator<T> validator, T? value)
        where T : IComparable
    {
        Comparer<T> defaultComparer = Comparer<T>.Default;

        ValidationException.ThrowIf(!(defaultComparer.Compare(validator.Value, value) == 0), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsEqualTo<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> value)
        where T : struct
    {
        Comparer<Nullable<T>> defaultComparer = Comparer<Nullable<T>>.Default;

        ValidationException.ThrowIf(!(defaultComparer.Compare(validator.Value, value) == 0), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsEqualTo<T>(this IValidator<Nullable<T>> validator,
        T value)
        where T : struct
    {
        Comparer<Nullable<T>> defaultComparer = Comparer<Nullable<T>>.Default;

        bool valueIsValid = (defaultComparer.Compare(validator.Value, value) == 0);

        ValidationException.ThrowIf(!valueIsValid, validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<T> IsNotEqualTo<T>(this IValidator<T> validator, T? value)
        where T : IComparable
    {
        ValidationException.ThrowIf(Comparer<T>.Default.Compare(validator.Value, value) == 0, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotEqualTo<T>(this IValidator<Nullable<T>> validator,
        Nullable<T> value)
        where T : struct
    {
        ValidationException.ThrowIf(Comparer<Nullable<T>>.Default.Compare(validator.Value, value) == 0, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Nullable<T>> IsNotEqualTo<T>(this IValidator<Nullable<T>> validator,
        T value)
        where T : struct
    {
        Comparer<Nullable<T>> comparer = Comparer<Nullable<T>>.Default;

        bool valueIsInvalid = comparer.Compare(validator.Value, value) == 0;

        ValidationException.ThrowIf(valueIsInvalid, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region bool
    /// <summary>
    /// Determines whether the specified validator is true.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<bool> IsTrue(this IValidator<bool> validator)
    {
        ValidationException.ThrowIf(!validator.Value, validator.Message, validator, ExceptionType.IsFalse);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is true.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<bool?> IsTrue(this IValidator<bool?> validator)
    {
        ValidationException.ThrowIf(!(validator.Value == true), validator.Message, validator, ExceptionType.IsFalse);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is false.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<bool> IsFalse(this IValidator<bool> validator)
    {
        ValidationException.ThrowIf(validator.Value, validator.Message, validator, ExceptionType.IsTrue);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is false.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<bool?> IsFalse(this IValidator<bool?> validator)
    {
        ValidationException.ThrowIf(!(validator.Value == false), validator.Message, validator, ExceptionType.IsTrue);



        return validator;
    }
    #endregion

    #region byte
    /// <summary>
    /// Determines whether the specified validator in byte is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsInRange(this IValidator<byte> validator, byte minValue, byte maxValue)
    {
        byte value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsNotInRange(this IValidator<byte> validator, byte minValue, byte maxValue)
    {
        byte value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsGreaterThan(this IValidator<byte> validator, byte minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsNotGreaterThan(this IValidator<byte> validator, byte maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsGreaterOrEqual(this IValidator<byte> validator, byte minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsNotGreaterOrEqual(this IValidator<byte> validator, byte maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsLessThan(this IValidator<byte> validator, byte maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is not less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsNotLessThan(this IValidator<byte> validator, byte minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsLessOrEqual(this IValidator<byte> validator, byte maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsNotLessOrEqual(this IValidator<byte> validator, byte minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsEqualTo(this IValidator<byte> validator, byte value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator in byte is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<byte> IsNotEqualTo(this IValidator<byte> validator, byte value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region DateTime
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsInRange(this IValidator<DateTime> validator, DateTime minValue, DateTime maxValue)
    {
        DateTime value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsNotInRange(this IValidator<DateTime> validator, DateTime minValue, DateTime maxValue)
    {
        DateTime value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsGreaterThan(this IValidator<DateTime> validator, DateTime minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsNotGreaterThan(this IValidator<DateTime> validator, DateTime maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsGreaterOrEqual(this IValidator<DateTime> validator, DateTime minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsNotGreaterOrEqual(this IValidator<DateTime> validator, DateTime maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsLessThan(this IValidator<DateTime> validator, DateTime maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsNotLessThan(this IValidator<DateTime> validator, DateTime minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsLessOrEqual(this IValidator<DateTime> validator, DateTime maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsNotLessOrEqual(this IValidator<DateTime> validator, DateTime minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsEqualTo(this IValidator<DateTime> validator, DateTime value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<DateTime> IsNotEqualTo(this IValidator<DateTime> validator, DateTime value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region Decimal
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsInRange(this IValidator<Decimal> validator, Decimal minValue, Decimal maxValue)
    {
        Decimal value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsNotInRange(this IValidator<Decimal> validator, Decimal minValue, Decimal maxValue)
    {
        Decimal value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsGreaterThan(this IValidator<Decimal> validator, Decimal minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsNotGreaterThan(this IValidator<Decimal> validator, Decimal maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsGreaterOrEqual(this IValidator<Decimal> validator, Decimal minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsNotGreaterOrEqual(this IValidator<Decimal> validator, Decimal maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsLessThan(this IValidator<Decimal> validator, Decimal maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsNotLessThan(this IValidator<Decimal> validator, Decimal minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsLessOrEqual(this IValidator<Decimal> validator, Decimal maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsNotLessOrEqual(this IValidator<Decimal> validator, Decimal minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsEqualTo(this IValidator<Decimal> validator, Decimal value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Decimal> IsNotEqualTo(this IValidator<Decimal> validator, Decimal value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region Double
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsInRange(this IValidator<Double> validator, Double minValue, Double maxValue)
    {
        Double value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);

        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsNotInRange(this IValidator<Double> validator, Double minValue, Double maxValue)
    {
        Double value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsGreaterThan(this IValidator<Double> validator, Double minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsNotGreaterThan(this IValidator<Double> validator, Double maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsGreaterOrEqual(this IValidator<Double> validator, Double minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsNotGreaterOrEqual(this IValidator<Double> validator, Double maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsLessThan(this IValidator<Double> validator, Double maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsNotLessThan(this IValidator<Double> validator, Double minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsLessOrEqual(this IValidator<Double> validator, Double maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsNotLessOrEqual(this IValidator<Double> validator, Double minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsEqualTo(this IValidator<Double> validator, Double value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<Double> IsNotEqualTo(this IValidator<Double> validator, Double value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region Int16
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<short> IsInRange(this IValidator<short> validator, short minValue, short maxValue)
    {
        short value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<short> IsNotInRange(this IValidator<short> validator, short minValue, short maxValue)
    {
        short value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<short> IsGreaterThan(this IValidator<short> validator, short minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<short> IsNotGreaterThan(this IValidator<short> validator, short maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<short> IsGreaterOrEqual(this IValidator<short> validator, short minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<short> IsNotGreaterOrEqual(this IValidator<short> validator, short maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<short> IsLessThan(this IValidator<short> validator, short maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<short> IsNotLessThan(this IValidator<short> validator, short minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<short> IsLessOrEqual(this IValidator<short> validator, short maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<short> IsNotLessOrEqual(this IValidator<short> validator, short minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<short> IsEqualTo(this IValidator<short> validator, short value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<short> IsNotEqualTo(this IValidator<short> validator, short value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region Int32
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<int> IsInRange(this IValidator<int> validator, int minValue, int maxValue)
    {
        int value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<int> IsNotInRange(this IValidator<int> validator, int minValue, int maxValue)
    {
        int value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<int> IsGreaterThan(this IValidator<int> validator, int minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<int> IsNotGreaterThan(this IValidator<int> validator, int maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<int> IsGreaterOrEqual(this IValidator<int> validator, int minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<int> IsNotGreaterOrEqual(this IValidator<int> validator, int maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<int> IsLessThan(this IValidator<int> validator, int maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is notless than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<int> IsNotLessThan(this IValidator<int> validator, int minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<int> IsLessOrEqual(this IValidator<int> validator, int maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<int> IsNotLessOrEqual(this IValidator<int> validator, int minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<int> IsEqualTo(this IValidator<int> validator, int value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<int> IsNotEqualTo(this IValidator<int> validator, int value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region Int64
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<long> IsInRange(this IValidator<long> validator, long minValue, long maxValue)
    {
        long value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<long> IsNotInRange(this IValidator<long> validator, long minValue, long maxValue)
    {
        long value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);


        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<long> IsGreaterThan(this IValidator<long> validator, long minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<long> IsNotGreaterThan(this IValidator<long> validator, long maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<long> IsGreaterOrEqual(this IValidator<long> validator, long minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<long> IsNotGreaterOrEqual(this IValidator<long> validator, long maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<long> IsLessThan(this IValidator<long> validator, long maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<long> IsNotLessThan(this IValidator<long> validator, long minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<long> IsLessOrEqual(this IValidator<long> validator, long maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<long> IsNotLessOrEqual(this IValidator<long> validator, long minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<long> IsEqualTo(this IValidator<long> validator, long value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<long> IsNotEqualTo(this IValidator<long> validator, long value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region Single
    /// <summary>
    /// Determines whether the specified validator is in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<float> IsInRange(this IValidator<float> validator, float minValue, float maxValue)
    {
        float value = validator.Value;

        ValidationException.ThrowIf(!(value >= minValue && value <= maxValue), validator.Message, validator, ExceptionType.IsNotInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not in range.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<float> IsNotInRange(this IValidator<float> validator, float minValue, float maxValue)
    {
        float value = validator.Value;

        ValidationException.ThrowIf(value >= minValue && value <= maxValue, validator.Message, validator, ExceptionType.IsInRange);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<float> IsGreaterThan(this IValidator<float> validator, float minValue)
    {
        ValidationException.ThrowIf(!(validator.Value > minValue), validator.Message, validator, ExceptionType.IsNotGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<float> IsNotGreaterThan(this IValidator<float> validator, float maxValue)
    {
        ValidationException.ThrowIf(validator.Value > maxValue, validator.Message, validator, ExceptionType.IsGreaterThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<float> IsGreaterOrEqual(this IValidator<float> validator, float minValue)
    {
        ValidationException.ThrowIf(!(validator.Value >= minValue), validator.Message, validator, ExceptionType.IsNotGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not greater than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<float> IsNotGreaterOrEqual(this IValidator<float> validator, float maxValue)
    {
        ValidationException.ThrowIf(validator.Value >= maxValue, validator.Message, validator, ExceptionType.IsGreaterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<float> IsLessThan(this IValidator<float> validator, float maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value < maxValue), validator.Message, validator, ExceptionType.IsNotLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<float> IsNotLessThan(this IValidator<float> validator, float minValue)
    {
        ValidationException.ThrowIf(validator.Value < minValue, validator.Message, validator, ExceptionType.IsLessThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="maxValue">The max value.</param>
    /// <returns></returns>
    public static IValidator<float> IsLessOrEqual(this IValidator<float> validator, float maxValue)
    {
        ValidationException.ThrowIf(!(validator.Value <= maxValue), validator.Message, validator, ExceptionType.IsNotLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not less than or equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="minValue">The min value.</param>
    /// <returns></returns>
    public static IValidator<float> IsNotLessOrEqual(this IValidator<float> validator, float minValue)
    {
        ValidationException.ThrowIf(validator.Value <= minValue, validator.Message, validator, ExceptionType.IsLessOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<float> IsEqualTo(this IValidator<float> validator, float value)
    {
        ValidationException.ThrowIf(!(validator.Value == value), validator.Message, validator, ExceptionType.IsNotEqualTo);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not equal to the given value.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IValidator<float> IsNotEqualTo(this IValidator<float> validator, float value)
    {
        ValidationException.ThrowIf(validator.Value == value, validator.Message, validator, ExceptionType.IsEqualTo);



        return validator;
    }
    #endregion

    #region Collection
    /// <summary>
    /// Determines whether the specified validator is empty.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsEmpty<TCollection>(this IValidator<TCollection> validator)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.IsSequenceNullOrEmpty(validator.Value), validator.Message, validator, ExceptionType.IsNotEmpty);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator is not empty.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsNotEmpty<TCollection>(this IValidator<TCollection> validator)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.IsSequenceNullOrEmpty(validator.Value), validator.Message, validator, ExceptionType.IsEmpty);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator contains the given element.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    public static IValidator<TCollection> Contains<TCollection, TElement>(
        this IValidator<TCollection> validator, TElement element)
        where TCollection : IEnumerable<TElement>
    {
        ValidationException.ThrowIf(validator.Value == null || !CollectionConditions.Contains<TElement>(validator.Value, element), validator.Message, validator, ExceptionType.DoesNotContain);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator contains the given element.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    public static IValidator<TCollection> Contains<TCollection>(this IValidator<TCollection> validator,
        object element)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(validator.Value == null || !CollectionConditions.Contains(validator.Value, element), validator.Message, validator, ExceptionType.DoesNotContain);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator does not contain the given element.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    public static IValidator<TCollection> DoesNotContain<TCollection, TElement>(
        this IValidator<TCollection> validator, TElement element)
        where TCollection : IEnumerable<TElement>
    {
        ValidationException.ThrowIf(validator.Value != null && Enumerable.Contains(validator.Value, element), validator.Message, validator, ExceptionType.Contains);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator does not contain the given element.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    public static IValidator<TCollection> DoesNotContain<TCollection>(
        this IValidator<TCollection> validator, object element)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(validator.Value != null && CollectionConditions.Contains(validator.Value, element), validator.Message, validator, ExceptionType.Contains);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator contains any element in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> ContainsAny<TCollection, TElement>(
        this IValidator<TCollection> validator, IEnumerable<TElement> elements)
        where TCollection : IEnumerable<TElement>
    {
        ValidationException.ThrowIf(!CollectionConditions.ContainsAny<TElement>(validator.Value, elements), validator.Message, validator, ExceptionType.DoesNotContainAny);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator contains any element in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> ContainsAny<TCollection>(this IValidator<TCollection> validator,
        IEnumerable elements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.ContainsAny(validator.Value, elements), validator.Message, validator, ExceptionType.DoesNotContainAny);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator does not contain any element in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> DoesNotContainAny<TCollection, TElement>(
        this IValidator<TCollection> validator, IEnumerable<TElement> elements)
        where TCollection : IEnumerable<TElement>
    {
        ValidationException.ThrowIf(CollectionConditions.ContainsAny<TElement>(validator.Value, elements), validator.Message, validator, ExceptionType.ContainsAny);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator does not contain any element in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> DoesNotContainAny<TCollection>(
        this IValidator<TCollection> validator, IEnumerable elements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.ContainsAny(validator.Value, elements), validator.Message, validator, ExceptionType.ContainsAny);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator contains all elements in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> ContainsAll<TCollection, TElement>(
        this IValidator<TCollection> validator, IEnumerable<TElement> elements)
        where TCollection : IEnumerable<TElement>
    {
        ValidationException.ThrowIf(!CollectionConditions.ContainsAll<TElement>(validator.Value, elements), validator.Message, validator, ExceptionType.DoesNotContainAll);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator contains all elements in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> ContainsAll<TCollection>(this IValidator<TCollection> validator,
        IEnumerable elements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.ContainsAll(validator.Value, elements), validator.Message, validator, ExceptionType.DoesNotContainAll);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator does not contain all elements in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> DoesNotContainAll<TCollection, TElement>(
        this IValidator<TCollection> validator, IEnumerable<TElement> elements)
        where TCollection : IEnumerable<TElement>
    {
        ValidationException.ThrowIf(CollectionConditions.ContainsAll<TElement>(validator.Value, elements), validator.Message, validator, ExceptionType.ContainsAll);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator does not contain all elements in the given collection.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="elements">The elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> DoesNotContainAll<TCollection>(
        this IValidator<TCollection> validator, IEnumerable elements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.ContainsAll(validator.Value, elements), validator.Message, validator, ExceptionType.ContainsAll);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator has a number of elements.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> HasLength<TCollection>(this IValidator<TCollection> validator,
        int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.SequenceHasLength(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.DoesNotHaveLength);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator does not have a number of elements.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> DoesNotHaveLength<TCollection>(
        this IValidator<TCollection> validator, int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.SequenceHasLength(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.HasLength);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator lenght is shorter than the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsShorterThan<TCollection>(this IValidator<TCollection> validator,
        int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.SequenceIsShorterThan(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsNotShorter);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator length is shorter than the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsNotShorterThan<TCollection>(
        this IValidator<TCollection> validator, int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.SequenceIsShorterThan(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsShorter);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator length is shorter or equal to the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsShorterOrEqual<TCollection>(
        this IValidator<TCollection> validator, int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.SequenceIsShorterOrEqual(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsNotShorterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator length is not shorter or equal to the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsNotShorterOrEqual<TCollection>(
        this IValidator<TCollection> validator, int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.SequenceIsShorterOrEqual(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsShorterOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator length is longer or equal to the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsLongerThan<TCollection>(this IValidator<TCollection> validator,
        int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.SequenceIsLongerThan(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsNotLongerThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator length is not longer or equal to the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsNotLongerThan<TCollection>(
       this IValidator<TCollection> validator, int numberOfElements)
       where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.SequenceIsLongerThan(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsLongerThan);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator length is longer or equal to the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsLongerOrEqual<TCollection>(
        this IValidator<TCollection> validator, int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(!CollectionConditions.SequenceIsLongerOrEqual(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsNotLongerOrEqual);



        return validator;
    }

    /// <summary>
    /// Determines whether the specified validator length is longer or equal to the given length.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="numberOfElements">The number of elements.</param>
    /// <returns></returns>
    public static IValidator<TCollection> IsNotLongerOrEqual<TCollection>(
        this IValidator<TCollection> validator, int numberOfElements)
        where TCollection : IEnumerable
    {
        ValidationException.ThrowIf(CollectionConditions.SequenceIsLongerOrEqual(validator.Value, numberOfElements), validator.Message, validator, ExceptionType.IsLongerOrEqual);



        return validator;
    }
    #endregion

}


internal static class CollectionConditions
{
    internal static bool Contains<TSource>(IEnumerable<TSource>? source, TSource value)
    {
        ICollection<TSource>? is2 = source as ICollection<TSource>;

        if (is2 is not null)
        {
            return is2.Contains(value);
        }

        IEqualityComparer<TSource> comparer = EqualityComparer<TSource>.Default;

        if(source is not null)
        foreach (TSource local in source)
        {
            if (comparer.Equals(local, value))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool Contains(IEnumerable? sequence, object value)
    {
        IList? list = sequence as IList;

        if (list is not null)
        {
            return list.Contains(value);
        }

        Comparer<object> comparer = Comparer<object>.Default;

        if(sequence is not null)
        foreach (object element in sequence)
        {
            if (comparer.Compare(element, value) == 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsAny<T>(IEnumerable<T>? sequence, IEnumerable<T>? values)
    {
        if (IsSequenceNullOrEmpty(values) || IsSequenceNullOrEmpty(sequence))
        {
            return false;
        }



        foreach (T element in values!)
        {
            if (sequence!.Contains(element))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsAny(IEnumerable? sequence, IEnumerable? values)
    {
        if (IsSequenceNullOrEmpty(values) || IsSequenceNullOrEmpty(sequence))
        {
            return false;
        }


        var commonElements = from object elem in values!.AsQueryable()
                             join object seek in sequence!.AsQueryable() on elem equals seek
                             select elem;

        if (commonElements.Any())
            return true;

        return false;
    }

    internal static bool ContainsAll<T>(IEnumerable<T>? sequence, IEnumerable<T>? values)
    {
        if (IsSequenceNullOrEmpty(values))
        {
            return true;
        }

        if (IsSequenceNullOrEmpty(sequence))
        {
            return false;
        }


        foreach (T element in values!)
        {
            var commonElements = from object seek in sequence!.AsQueryable()
                                 where seek.Equals(element)
                                 select seek;

            if (!commonElements.Any())
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ContainsAll(IEnumerable? collection, IEnumerable? values)
    {
        if (IsSequenceNullOrEmpty(values))
        {
            return true;
        }

        if (IsSequenceNullOrEmpty(collection))
        {
            return false;
        }


        foreach (object element in values!)
        {
            var commonElements = from object seek in collection!.AsQueryable()
                                 where seek.Equals(element) == true
                                 select seek;

            if (!commonElements.Any())
            {
                return false;
            }
        }

        return true;
    }

    internal static bool SequenceHasLength(IEnumerable? sequence, int numberOfElements)
    {
        if (sequence is null)
        {
            return 0 == numberOfElements;
        }

        ICollection? collection = sequence as ICollection;

        if (collection is not null)
        {
            return collection.Count == numberOfElements;
        }

        IEnumerator enumerator = sequence.GetEnumerator();
        try
        {
            int lengthOfSequence = 0;

            while (enumerator.MoveNext())
            {
                lengthOfSequence++;

                if (lengthOfSequence > numberOfElements)
                {
                    return false;
                }
            }

            return lengthOfSequence == numberOfElements;
        }
        finally
        {
            IDisposable? disposable = enumerator as IDisposable;
            if (disposable is not null)
            {
                disposable.Dispose();
            }
        }
    }

    internal static bool IsSequenceNullOrEmpty<TSource>(IEnumerable<TSource>? sequence)
    {
        if (sequence is null)
        {
            return true;
        }

        ICollection<TSource>? collection = sequence as ICollection<TSource>;

        if (collection is not null)
        {

            return collection.Count == 0;
        }
        else
        {

            return IsSequenceNullOrEmpty((IEnumerable)sequence);
        }
    }

    internal static bool IsSequenceNullOrEmpty(IEnumerable? sequence)
    {
        if (sequence is null)
        {
            return true;
        }

        ICollection? collection = sequence as ICollection;

        if (collection is not null)
        {
            return collection.Count == 0;
        }
        else
        {
            return IsEnumerableEmpty(sequence);
        }
    }

    internal static bool SequenceIsShorterThan(IEnumerable? sequence, int numberOfElements)
    {
        if (sequence is null)
        {
            return 0 < numberOfElements;
        }

        ICollection? collection = sequence as ICollection;

        if (collection is not null)
        {
            return collection.Count < numberOfElements;
        }

        IEnumerator enumerator = sequence.GetEnumerator();
        try
        {
            int lengthOfSequence = 0;

            while (enumerator.MoveNext())
            {
                lengthOfSequence++;

                if (lengthOfSequence >= numberOfElements)
                {
                    return false;
                }
            }

            return lengthOfSequence < numberOfElements;
        }
        finally
        {
            IDisposable? disposable = enumerator as IDisposable;
            if (disposable is not null)
            {
                disposable.Dispose();
            }
        }
    }

    internal static bool SequenceIsShorterOrEqual(IEnumerable? sequence, int numberOfElements)
    {
        if (sequence is null)
        {
            return 0 <= numberOfElements;
        }

        ICollection? collection = sequence as ICollection;

        if (collection is not null)
        {
            return collection.Count <= numberOfElements;
        }

        IEnumerator enumerator = sequence.GetEnumerator();
        try
        {
            int lengthOfSequence = 0;

            while (enumerator.MoveNext())
            {
                lengthOfSequence++;

                if (lengthOfSequence > numberOfElements)
                {
                    return false;
                }
            }

            return lengthOfSequence <= numberOfElements;
        }
        finally
        {
            IDisposable? disposable = enumerator as IDisposable;
            if (disposable is not null)
            {
                disposable.Dispose();
            }
        }
    }

    internal static bool SequenceIsLongerThan(IEnumerable? sequence, int numberOfElements)
    {
        if (sequence is null)
        {
            return 0 > numberOfElements;
        }

        ICollection? collection = sequence as ICollection;

        if (collection is not null)
        {
            return collection.Count > numberOfElements;
        }

        IEnumerator enumerator = sequence.GetEnumerator();
        try
        {
            int lengthOfSequence = 0;

            while (enumerator.MoveNext())
            {
                lengthOfSequence++;

                if (lengthOfSequence > numberOfElements)
                {
                    return true;
                }
            }

            return lengthOfSequence > numberOfElements;
        }
        finally
        {
            IDisposable? disposable = enumerator as IDisposable;
            if (disposable is not null)
            {
                disposable.Dispose();
            }
        }
    }

    internal static bool SequenceIsLongerOrEqual(IEnumerable? sequence, int numberOfElements)
    {
        if (sequence is null)
        {
            return 0 >= numberOfElements;
        }

        ICollection? collection = sequence as ICollection;

        if (collection is not null)
        {
            return collection.Count >= numberOfElements;
        }

        IEnumerator enumerator = sequence.GetEnumerator();
        try
        {
            int lengthOfSequence = 0;

            while (enumerator.MoveNext())
            {
                lengthOfSequence++;

                if (lengthOfSequence >= numberOfElements)
                {
                    return true;
                }
            }

            return lengthOfSequence >= numberOfElements;
        }
        finally
        {
            IDisposable? disposable = enumerator as IDisposable;
            if (disposable is not null)
            {
                disposable.Dispose();
            }
        }
    }

    private static bool IsEnumerableEmpty(IEnumerable sequence)
    {
        IEnumerator enumerator = sequence.GetEnumerator();

        try
        {
            return !enumerator.MoveNext();
        }
        finally
        {
            IDisposable? disposable = enumerator as IDisposable;
            if (disposable is not null)
            {
                disposable.Dispose();
            }
        }
    }
}
