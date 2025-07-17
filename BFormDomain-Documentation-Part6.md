# BFormDomain Comprehensive Documentation - Part 6

## Validation Framework

The Validation Framework provides a fluent, expressive API for validating data throughout BFormDomain. It supports both precondition checking (Requires) and postcondition verification (Guarantees) with comprehensive validation rules and custom error handling.

### Core Components

#### IValidator Interface
Base validator interface with generic type support:

```csharp
public interface IValidator<T>
{
    T? Value { get; }
    string? ArgumentName { get; }
    string? Message { get; }
    
    void Initialize(T? value, string? argumentName, string? message);
    IValidator<T> Otherwise<TException>(TException ex) where TException : Exception;
    Exception BuildValidationException(string? message, ExceptionType exceptionType);
}
```

#### ExceptionType Enum
Comprehensive set of validation exception types:

```csharp
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
```

### Validator Extensions

#### Requires Validator
For precondition validation:

```csharp
public static class ValidatorExtensions
{
    // Basic requires
    public static IValidator<T> Requires<T>(this T? value)
    {
        return new RequiresValidator<T>(value, "value", $"requirement failed for {value}");
    }
    
    // Requires with custom message
    public static IValidator<T> Requires<T>(this T value, string message)
    {
        return new RequiresValidator<T>(value, "value", message);
    }
    
    // Requires with argument name and message
    public static IValidator<T> Requires<T>(this T value, string argumentName, string message)
    {
        return new RequiresValidator<T>(value, argumentName, message);
    }
}
```

#### Guarantees Validator
For postcondition validation:

```csharp
public static class ValidatorExtensions
{
    // Basic guarantees
    public static IValidator<T> Guarantees<T>(this T? value)
    {
        return new GuaranteesValidator<T>(value, "value", $"guarantee failed for value {value}");
    }
    
    // Guarantees with custom message
    public static IValidator<T> Guarantees<T>(this T? value, string? message)
    {
        return new GuaranteesValidator<T>(value!, "value", message);
    }
}
```

### Validation Rules

#### Null Validation

```csharp
// Null checks
public static IValidator<T> IsNull<T>(this IValidator<T> validator, string? message = null)
{
    ValidationException.ThrowIf(
        validator.Value != null,
        message ?? $"{validator.ArgumentName} must be null",
        validator,
        ExceptionType.IsNULL);
    return validator;
}

public static IValidator<T> IsNotNull<T>(this IValidator<T> validator, string? message = null)
{
    ValidationException.ThrowIf(
        validator.Value == null,
        message ?? $"{validator.ArgumentName} cannot be null",
        validator,
        ExceptionType.IsNotNULL);
    return validator;
}
```

#### String Validation

```csharp
// String validation extensions
public static IValidator<string> IsNotNullOrEmpty(
    this IValidator<string> validator, 
    string? message = null)
{
    ValidationException.ThrowIf(
        string.IsNullOrEmpty(validator.Value),
        message ?? $"{validator.ArgumentName} cannot be null or empty",
        validator,
        ExceptionType.IsNotNullOrEmpty);
    return validator;
}

public static IValidator<string> HasLength(
    this IValidator<string> validator, 
    int length, 
    string? message = null)
{
    ValidationException.ThrowIf(
        validator.Value?.Length != length,
        message ?? $"{validator.ArgumentName} must have length {length}",
        validator,
        ExceptionType.HasLength);
    return validator;
}

public static IValidator<string> StartsWith(
    this IValidator<string> validator, 
    string value, 
    string? message = null)
{
    ValidationException.ThrowIf(
        !validator.Value?.StartsWith(value) ?? true,
        message ?? $"{validator.ArgumentName} must start with '{value}'",
        validator,
        ExceptionType.StartsWith);
    return validator;
}
```

#### Numeric Validation

```csharp
// Numeric validation
public static IValidator<T> IsGreaterThan<T>(
    this IValidator<T> validator, 
    T value, 
    string? message = null) 
    where T : IComparable<T>
{
    ValidationException.ThrowIf(
        validator.Value?.CompareTo(value) <= 0,
        message ?? $"{validator.ArgumentName} must be greater than {value}",
        validator,
        ExceptionType.IsGreaterThan);
    return validator;
}

public static IValidator<T> IsInRange<T>(
    this IValidator<T> validator, 
    T min, 
    T max, 
    string? message = null) 
    where T : IComparable<T>
{
    ValidationException.ThrowIf(
        validator.Value?.CompareTo(min) < 0 || validator.Value?.CompareTo(max) > 0,
        message ?? $"{validator.ArgumentName} must be between {min} and {max}",
        validator,
        ExceptionType.IsInRange);
    return validator;
}
```

#### Collection Validation

```csharp
// Collection validation
public static IValidator<T> IsNotEmpty<T>(
    this IValidator<T> validator, 
    string? message = null) 
    where T : IEnumerable
{
    ValidationException.ThrowIf(
        !validator.Value?.Cast<object>().Any() ?? true,
        message ?? $"{validator.ArgumentName} cannot be empty",
        validator,
        ExceptionType.IsNotEmpty);
    return validator;
}

public static IValidator<T> Contains<T, TItem>(
    this IValidator<T> validator, 
    TItem item, 
    string? message = null) 
    where T : IEnumerable<TItem>
{
    ValidationException.ThrowIf(
        !validator.Value?.Contains(item) ?? true,
        message ?? $"{validator.ArgumentName} must contain {item}",
        validator,
        ExceptionType.Contains);
    return validator;
}
```

#### Custom Evaluation

```csharp
// Custom expression evaluation
public static IValidator<T> Evaluate<T>(
    this IValidator<T> validator,
    Expression<Func<T?, bool>> expression,
    string expressionDescription)
{
    var func = expression.Compile();
    ValidationException.ThrowIf(
        !func(validator.Value),
        $"{validator.ArgumentName} failed validation: {expressionDescription}",
        validator,
        ExceptionType.Evaluate);
    return validator;
}
```

### Usage Examples

#### Basic Parameter Validation

```csharp
public class UserService
{
    public async Task<User> CreateUserAsync(string email, string password, int age)
    {
        // Validate parameters
        email.Requires(nameof(email), "Email is required")
            .IsNotNullOrEmpty()
            .Contains("@", "Email must contain @")
            .DoesNotStartWith("admin", "Cannot create admin users this way");
            
        password.Requires(nameof(password), "Password is required")
            .IsNotNullOrEmpty()
            .HasMinLength(8, "Password must be at least 8 characters")
            .Evaluate(p => p!.Any(char.IsDigit), "Password must contain a number")
            .Evaluate(p => p!.Any(char.IsUpper), "Password must contain uppercase letter");
            
        age.Requires(nameof(age), "Age is required")
            .IsGreaterThan(0, "Age must be positive")
            .IsLessThan(150, "Age must be realistic")
            .IsInRange(18, 120, "User must be between 18 and 120 years old");
            
        // Create user
        return await CreateUserInternalAsync(email, password, age);
    }
}
```

#### Entity Validation

```csharp
public class WorkItem : IAppEntity
{
    private string _title = "";
    public string Title 
    { 
        get => _title;
        set
        {
            value.Requires(nameof(Title), "Title is required")
                .IsNotNullOrEmpty("Title cannot be empty")
                .HasMaxLength(200, "Title cannot exceed 200 characters");
            _title = value;
        }
    }
    
    private DateTime _dueDate;
    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            value.Requires(nameof(DueDate), "Due date is required")
                .Evaluate(d => d > DateTime.UtcNow, "Due date must be in the future")
                .Evaluate(d => d < DateTime.UtcNow.AddYears(1), "Due date must be within 1 year");
            _dueDate = value;
        }
    }
}
```

#### Complex Business Rules

```csharp
public class OrderValidator
{
    public void ValidateOrder(Order order)
    {
        order.Requires(nameof(order), "Order is required")
            .IsNotNull();
            
        order.CustomerId.Requires("CustomerId", "Customer is required")
            .IsNotEqualTo(Guid.Empty);
            
        order.Items.Requires("Items", "Order must have items")
            .IsNotNull()
            .IsNotEmpty("Order must contain at least one item")
            .Evaluate(items => items!.All(i => i.Quantity > 0), 
                "All items must have positive quantity")
            .Evaluate(items => items!.Sum(i => i.Price * i.Quantity) > 0,
                "Order total must be greater than zero");
                
        // Validate shipping
        if (order.RequiresShipping)
        {
            order.ShippingAddress.Requires("ShippingAddress", "Shipping address required")
                .IsNotNull();
                
            order.ShippingAddress!.Street.Requires("Street")
                .IsNotNullOrEmpty();
                
            order.ShippingAddress.PostalCode.Requires("PostalCode")
                .IsNotNullOrEmpty()
                .Evaluate(pc => IsValidPostalCode(pc!), "Invalid postal code format");
        }
    }
}
```

#### Custom Validators

```csharp
public static class CustomValidationExtensions
{
    public static IValidator<string> IsValidEmail(
        this IValidator<string> validator, 
        string? message = null)
    {
        var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return validator.Evaluate(
            email => !string.IsNullOrEmpty(email) && 
                     System.Text.RegularExpressions.Regex.IsMatch(email, emailRegex),
            message ?? "Invalid email format");
    }
    
    public static IValidator<string> IsValidPhoneNumber(
        this IValidator<string> validator,
        string? message = null)
    {
        return validator.Evaluate(
            phone => !string.IsNullOrEmpty(phone) && 
                     phone.All(c => char.IsDigit(c) || c == '-' || c == ' ') &&
                     phone.Count(char.IsDigit) >= 10,
            message ?? "Invalid phone number format");
    }
    
    public static IValidator<DateTime> IsBusinessDay(
        this IValidator<DateTime> validator,
        string? message = null)
    {
        return validator.Evaluate(
            date => date.DayOfWeek != DayOfWeek.Saturday && 
                    date.DayOfWeek != DayOfWeek.Sunday,
            message ?? "Date must be a business day");
    }
}
```

#### Validation with Custom Exceptions

```csharp
public class SecurityValidator
{
    public void ValidateAccess(User user, Resource resource)
    {
        user.Requires(nameof(user))
            .IsNotNull()
            .Otherwise(new UnauthorizedException("User not authenticated"));
            
        user.IsActive.Requires("User.IsActive")
            .IsTrue("User account is not active")
            .Otherwise(new AccountDisabledException(user.Id));
            
        user.Permissions.Requires("User.Permissions")
            .IsNotNull()
            .Contains(resource.RequiredPermission, 
                $"User lacks permission: {resource.RequiredPermission}")
            .Otherwise(new InsufficientPermissionsException(
                user.Id, 
                resource.RequiredPermission));
    }
}
```

#### Postcondition Validation

```csharp
public class CalculationService
{
    public decimal CalculateDiscount(decimal price, decimal discountPercent)
    {
        // Preconditions
        price.Requires(nameof(price))
            .IsGreaterOrEqual(0, "Price cannot be negative");
            
        discountPercent.Requires(nameof(discountPercent))
            .IsInRange(0, 100, "Discount must be between 0 and 100");
            
        // Calculate
        var discountAmount = price * (discountPercent / 100);
        var finalPrice = price - discountAmount;
        
        // Postconditions
        finalPrice.Guarantees("finalPrice")
            .IsGreaterOrEqual(0, "Final price calculation error")
            .IsLessOrEqual(price, "Discount cannot increase price");
            
        discountAmount.Guarantees("discountAmount")
            .IsGreaterOrEqual(0, "Discount amount must be positive")
            .Evaluate(d => Math.Abs(d - (price * discountPercent / 100)) < 0.01m,
                "Discount calculation error");
                
        return finalPrice;
    }
}
```

### Validation Patterns

#### Fluent Validation Chains

```csharp
public void ValidateFormData(FormInstance form)
{
    form.Requires()
        .IsNotNull()
        .Evaluate(f => f!.Fields != null, "Form must have fields")
        .Evaluate(f => f!.Fields.Any(), "Form must have at least one field")
        .Evaluate(f => f!.Fields.All(field => !string.IsNullOrEmpty(field.Name)),
            "All fields must have names")
        .Evaluate(f => f!.Fields.Select(field => field.Name).Distinct().Count() == 
                      f.Fields.Count,
            "Field names must be unique");
}
```

#### Conditional Validation

```csharp
public void ValidatePayment(Payment payment)
{
    payment.Requires().IsNotNull();
    
    // Base validation
    payment.Amount.Requires()
        .IsGreaterThan(0, "Payment amount must be positive");
        
    // Conditional validation based on payment type
    switch (payment.Type)
    {
        case PaymentType.CreditCard:
            payment.CardNumber.Requires()
                .IsNotNullOrEmpty()
                .HasLength(16, "Card number must be 16 digits")
                .Evaluate(cn => cn!.All(char.IsDigit), "Card number must be numeric");
                
            payment.CVV.Requires()
                .IsNotNullOrEmpty()
                .HasLength(3, "CVV must be 3 digits");
            break;
            
        case PaymentType.BankTransfer:
            payment.AccountNumber.Requires()
                .IsNotNullOrEmpty()
                .HasMinLength(10, "Account number too short");
                
            payment.RoutingNumber.Requires()
                .IsNotNullOrEmpty()
                .HasLength(9, "Routing number must be 9 digits");
            break;
    }
}
```

### Best Practices

1. **Use descriptive error messages** - Help developers understand what went wrong
2. **Validate at boundaries** - Check inputs at service/API boundaries
3. **Fail fast** - Validate early to catch issues sooner
4. **Chain validations logically** - Order from general to specific
5. **Create custom extensions** - Build domain-specific validators
6. **Use Otherwise for custom exceptions** - Throw appropriate exception types
7. **Validate postconditions** - Use Guarantees for output validation
8. **Keep validation DRY** - Create reusable validation methods
9. **Consider performance** - Cache compiled expressions
10. **Document validation rules** - Make requirements clear

---

## Utility Components

The Utility Components provide a comprehensive set of helper classes and extension methods that support common programming patterns throughout BFormDomain. These utilities handle async operations, caching, JSON manipulation, retry logic, and more.

### AsyncHelper

Safely runs async code in synchronous contexts:

```csharp
public static class AsyncHelper
{
    private static readonly TaskFactory _myTaskFactory = new(
        CancellationToken.None,
        TaskCreationOptions.None, 
        TaskContinuationOptions.None, 
        TaskScheduler.Default);

    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        var cultureUi = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.CurrentCulture;
        return _myTaskFactory.StartNew(() =>
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = cultureUi;
            return func();
        }).Unwrap().GetAwaiter().GetResult();
    }
    
    public static void RunSync(Func<Task> func)
    {
        var cultureUi = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.CurrentCulture;
        _myTaskFactory.StartNew(() =>
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = cultureUi;
            return func();
        }).Unwrap().GetAwaiter().GetResult();
    }
}
```

#### Usage Example:

```csharp
// When you need to call async code from a synchronous context
public User GetUserSync(Guid userId)
{
    return AsyncHelper.RunSync(async () => 
    {
        return await _userRepository.GetByIdAsync(userId);
    });
}
```

### JsonWinnower

Advanced JSON querying and manipulation using JSONPath with aggregation support:

```csharp
public class JsonWinnower
{
    public List<JsonPathWinnow> Winnows { get; set; } = new();
    public string AppendixProperty { get; set; } = "Appendix";
    public List<JToken> Final { get; private set; } = new();
    
    public void WinnowData(JObject sourceData)
    {
        // Execute winnowing steps with substitutions and aggregations
    }
}

public record JsonPathWinnow(
    string jsonPath,
    bool errorIfNoMatch = false,
    JArraySummarize? summarize = null,
    string? asSub = null,
    string? appendTo = null);

public enum JArraySummarize
{
    Min, Max, Sum, Mean, Median, Count
}
```

#### Usage Example:

```csharp
// Find max value in a column, then find the row with that max value
var winnower = new JsonWinnower()
    .Plan(new JsonPathWinnow(
        "$.data[*].score",
        summarize: JArraySummarize.Max,
        asSub: "maxScore"))
    .Plan(new JsonPathWinnow(
        "$.data[?(@.score == {maxScore})]",
        errorIfNoMatch: true));

winnower.WinnowData(tableData);
var topScoringRow = winnower.Final.First();
```

### Retry Utility

Implements retry logic with exponential backoff:

```csharp
public static class Retry
{
    public static async Task<T> DoAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 3,
        int delayMilliseconds = 1000,
        double backoffMultiplier = 2.0,
        Func<Exception, bool>? shouldRetry = null)
    {
        int attempt = 0;
        int currentDelay = delayMilliseconds;
        
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxRetries && 
                                      (shouldRetry?.Invoke(ex) ?? true))
            {
                attempt++;
                await Task.Delay(currentDelay);
                currentDelay = (int)(currentDelay * backoffMultiplier);
            }
        }
    }
}
```

#### Usage Example:

```csharp
// Retry API call with exponential backoff
var result = await Retry.DoAsync(
    async () => await httpClient.GetAsync(apiUrl),
    maxRetries: 5,
    delayMilliseconds: 500,
    shouldRetry: ex => ex is HttpRequestException || 
                       ex is TaskCanceledException);
```

### TemporalCollocator

Groups time-based data into buckets:

```csharp
public class TemporalCollocator<T>
{
    private readonly Func<T, DateTime> _timeExtractor;
    private readonly TimeSpan _bucketSize;
    
    public TemporalCollocator(
        Func<T, DateTime> timeExtractor, 
        TimeSpan bucketSize)
    {
        _timeExtractor = timeExtractor;
        _bucketSize = bucketSize;
    }
    
    public IEnumerable<IGrouping<DateTime, T>> Collocate(IEnumerable<T> items)
    {
        return items.GroupBy(item =>
        {
            var time = _timeExtractor(item);
            var ticks = time.Ticks / _bucketSize.Ticks;
            return new DateTime(ticks * _bucketSize.Ticks);
        });
    }
}
```

#### Usage Example:

```csharp
// Group events into 5-minute buckets
var collocator = new TemporalCollocator<Event>(
    e => e.Timestamp,
    TimeSpan.FromMinutes(5));

var eventBuckets = collocator.Collocate(events);
foreach (var bucket in eventBuckets)
{
    Console.WriteLine($"Bucket {bucket.Key}: {bucket.Count()} events");
}
```

### ExpressionEvaluator

Evaluates mathematical and logical expressions:

```csharp
public class ExpressionEvaluator
{
    private readonly Dictionary<string, object> _variables = new();
    
    public void SetVariable(string name, object value)
    {
        _variables[name] = value;
    }
    
    public T Evaluate<T>(string expression)
    {
        // Parse and evaluate expression with variables
        var lambda = DynamicExpressionParser.ParseLambda(
            _variables.Keys.Select(k => Expression.Parameter(typeof(object), k)).ToArray(),
            typeof(T),
            expression);
            
        return (T)lambda.Compile().DynamicInvoke(
            _variables.Values.ToArray());
    }
}
```

#### Usage Example:

```csharp
var evaluator = new ExpressionEvaluator();
evaluator.SetVariable("price", 100);
evaluator.SetVariable("discount", 0.2);

var finalPrice = evaluator.Evaluate<decimal>("price * (1 - discount)");
// Result: 80
```

### Caching Utilities

#### InMemoryCachedData

Thread-safe in-memory cache with expiration:

```csharp
public class InMemoryCachedData<TKey, TValue> : ICachedData<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache = new();
    private readonly TimeSpan _defaultExpiration;
    
    public async Task<TValue> GetOrAddAsync(
        TKey key,
        Func<Task<TValue>> factory,
        TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            return entry.Value;
        }
        
        var value = await factory();
        var newEntry = new CacheEntry<TValue>(
            value, 
            DateTime.UtcNow.Add(expiration ?? _defaultExpiration));
            
        _cache.AddOrUpdate(key, newEntry, (k, old) => newEntry);
        return value;
    }
}
```

### Extension Methods

#### EnumerableExtensions

```csharp
public static class EnumerableEx
{
    // Partition collection into chunks
    public static IEnumerable<IEnumerable<T>> Partition<T>(
        this IEnumerable<T> source, 
        int size)
    {
        var list = new List<T>(size);
        foreach (var item in source)
        {
            list.Add(item);
            if (list.Count == size)
            {
                yield return list;
                list = new List<T>(size);
            }
        }
        if (list.Any())
            yield return list;
    }
    
    // Safe enumeration with null check
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source)
    {
        return source ?? Enumerable.Empty<T>();
    }
}
```

#### ExceptionExtensions

```csharp
public static class ExceptionExtensions
{
    public static string TraceInformation(this Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        var level = 0;
        
        while (current != null)
        {
            sb.AppendLine($"{new string(' ', level * 2)}{current.GetType().Name}: {current.Message}");
            sb.AppendLine($"{new string(' ', level * 2)}Stack: {current.StackTrace}");
            
            current = current.InnerException;
            level++;
        }
        
        return sb.ToString();
    }
}
```

### RunOnce Utility

Ensures code runs only once:

```csharp
public static class RunOnce
{
    private static readonly ConcurrentDictionary<string, bool> _executed = new();
    
    public static void ThisCode(Action code, [CallerMemberName] string key = "")
    {
        if (_executed.TryAdd(key, true))
        {
            code();
        }
    }
    
    public static async Task ThisCodeAsync(
        Func<Task> code, 
        [CallerMemberName] string key = "")
    {
        if (_executed.TryAdd(key, true))
        {
            await code();
        }
    }
}
```

#### Usage Example:

```csharp
public class StartupService
{
    public void Initialize()
    {
        RunOnce.ThisCode(() =>
        {
            // This runs only once, even if Initialize is called multiple times
            ConfigureDatabase();
            LoadConfiguration();
        });
    }
}
```

### Disposable Utilities

```csharp
public static class Disposable
{
    // Create disposable from action
    public static IDisposable Create(Action disposeAction)
    {
        return new AnonymousDisposable(disposeAction);
    }
    
    // Combine multiple disposables
    public static IDisposable Combine(params IDisposable[] disposables)
    {
        return Create(() =>
        {
            foreach (var d in disposables)
                d?.Dispose();
        });
    }
}
```

### GUID Encoding

```csharp
public static class GuidEncoder
{
    // Convert GUID to short string
    public static string Encode(Guid guid)
    {
        string encoded = Convert.ToBase64String(guid.ToByteArray());
        encoded = encoded
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");
        return encoded;
    }
    
    // Convert short string back to GUID
    public static Guid Decode(string encoded)
    {
        encoded = encoded
            .Replace("_", "/")
            .Replace("-", "+");
            
        switch (encoded.Length % 4)
        {
            case 2: encoded += "=="; break;
            case 3: encoded += "="; break;
        }
        
        byte[] buffer = Convert.FromBase64String(encoded);
        return new Guid(buffer);
    }
}
```

### Multimap Collection

```csharp
public class Multimap<TKey, TValue> : Dictionary<TKey, List<TValue>>
{
    public void Add(TKey key, TValue value)
    {
        if (!TryGetValue(key, out var list))
        {
            list = new List<TValue>();
            base[key] = list;
        }
        list.Add(value);
    }
    
    public void AddRange(TKey key, IEnumerable<TValue> values)
    {
        foreach (var value in values)
            Add(key, value);
    }
    
    public IEnumerable<TValue> GetValues(TKey key)
    {
        return TryGetValue(key, out var list) 
            ? list 
            : Enumerable.Empty<TValue>();
    }
}
```

### Usage Examples

#### Complex Data Processing

```csharp
public class DataProcessor
{
    public async Task<ProcessingResult> ProcessDataAsync(DataSet data)
    {
        // Use retry for external API calls
        var enrichedData = await Retry.DoAsync(
            async () => await EnrichDataFromApiAsync(data),
            maxRetries: 3);
            
        // Group data by time windows
        var temporalGroups = new TemporalCollocator<DataPoint>(
            dp => dp.Timestamp,
            TimeSpan.FromHours(1))
            .Collocate(enrichedData.Points);
            
        // Process in parallel batches
        var results = await temporalGroups
            .Partition(10)
            .SelectManyAsync(async batch =>
            {
                return await ProcessBatchAsync(batch);
            });
            
        // Cache results
        await _cache.GetOrAddAsync(
            data.Id,
            async () => results,
            TimeSpan.FromMinutes(30));
            
        return results;
    }
}
```

#### JSON Data Analysis

```csharp
public class JsonAnalyzer
{
    public AnalysisResult AnalyzeTableData(JObject tableData)
    {
        var winnower = new JsonWinnower();
        
        // Find average score
        winnower.Plan(new JsonPathWinnow(
            "$.rows[*].score",
            summarize: JArraySummarize.Mean,
            appendTo: "avgScore"));
            
        // Find rows above average
        winnower.Plan(new JsonPathWinnow(
            "$.rows[?(@.score > $.Appendix.avgScore)]",
            errorIfNoMatch: false));
            
        winnower.WinnowData(tableData);
        
        return new AnalysisResult
        {
            AverageScore = (double)tableData["Appendix"]["avgScore"],
            AboveAverageRows = winnower.Final.ToList()
        };
    }
}
```

### Best Practices

1. **Use AsyncHelper sparingly** - Prefer async all the way down
2. **Configure retry policies appropriately** - Consider circuit breakers for external services
3. **Set reasonable cache expiration** - Balance freshness with performance
4. **Handle null collections** - Use EmptyIfNull extension
5. **Log exceptions with context** - Use TraceInformation for detailed logs
6. **Partition large datasets** - Process in manageable chunks
7. **Use RunOnce for initialization** - Avoid duplicate setup
8. **Dispose resources properly** - Use Disposable.Combine for multiple resources
9. **Encode GUIDs for URLs** - Use GuidEncoder for shorter strings
10. **Test edge cases** - Especially for time-based utilities

---

## Plugin Architecture and Extension Points

BFormDomain provides a comprehensive plugin architecture that allows extending functionality without modifying core code. The system uses interfaces, attributes, and dependency injection to support plugins for rule actions, entity loaders, validators, and more.

### Core Plugin Interfaces

#### IRuleAction Plugin
The most common extension point for adding custom business logic:

```csharp
[RuleAction("my-custom-action")]
public class MyCustomRuleAction : IRuleAction
{
    private readonly IRepository<MyEntity> _repository;
    private readonly INotificationService _notifications;
    
    public MyCustomRuleAction(
        IRepository<MyEntity> repository,
        INotificationService notifications)
    {
        _repository = repository;
        _notifications = notifications;
    }
    
    public async Task<RuleActionResponse> ExecuteAsync(
        Rule rule,
        AppEvent appEvent,
        RuleActionParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract data from event
            var entityId = appEvent.EventData.Get<Guid>("entityId");
            var action = parameters.Get<string>("action");
            
            // Perform custom logic
            var entity = await _repository.GetByIdAsync(entityId);
            await ProcessEntityAsync(entity, action);
            
            // Return success
            return RuleActionResponse.Success(new
            {
                processed = true,
                entityId = entityId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return RuleActionResponse.Failure(ex.Message);
        }
    }
}
```

#### IEntityLoaderModule Plugin
For custom entity loading and reference resolution:

```csharp
public class CustomEntityLoaderModule : IEntityLoaderModule
{
    private readonly IRepository<CustomEntity> _repository;
    
    public IEnumerable<string> EntityTypes => new[] { "CustomEntity" };
    
    public async Task<object?> GetEntityAsync(
        string entityType, 
        Guid id, 
        IEntityLoaderContext context)
    {
        if (entityType != "CustomEntity")
            return null;
            
        var (entity, version) = await _repository.GetByIdAsync(id);
        
        // Apply security filtering if needed
        if (context.UserId.HasValue && !CanUserAccess(context.UserId.Value, entity))
            return null;
            
        return entity;
    }
    
    public Uri MakeReference(string entityType, Guid id, Dictionary<string, string> parameters)
    {
        var builder = new UriBuilder($"custom://{entityType}/{id}");
        
        if (parameters.Any())
        {
            builder.Query = string.Join("&", 
                parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        }
        
        return builder.Uri;
    }
}
```

#### IEventAppender Plugin
For augmenting events with additional data:

```csharp
[EventAppender("enrich-user-data")]
public class UserDataEventAppender : IEventAppender
{
    private readonly IUserService _userService;
    
    public async Task<AppEvent> AppendAsync(
        AppEvent sourceEvent,
        EventAppenderParameters parameters)
    {
        // Only process events with userId
        if (!sourceEvent.EventData.ContainsKey("userId"))
            return sourceEvent;
            
        var userId = sourceEvent.EventData.Get<Guid>("userId");
        var user = await _userService.GetUserAsync(userId);
        
        // Create enriched event
        var enrichedData = new Dictionary<string, object?>(sourceEvent.EventData);
        enrichedData["userName"] = user.DisplayName;
        enrichedData["userEmail"] = user.Email;
        enrichedData["userRoles"] = user.Roles;
        
        return sourceEvent with
        {
            EventData = enrichedData,
            Topic = parameters.Get<string>("targetTopic", sourceEvent.Topic)
        };
    }
}
```

### Plugin Registration

#### Automatic Discovery
Using assembly scanning:

```csharp
public class PluginRegistration
{
    public static void RegisterPlugins(IServiceCollection services)
    {
        var pluginAssemblies = new[]
        {
            Assembly.GetExecutingAssembly(),
            Assembly.Load("MyCompany.BForm.Plugins")
        };
        
        // Register all rule actions
        foreach (var assembly in pluginAssemblies)
        {
            var ruleActionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => typeof(IRuleAction).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttribute<RuleActionAttribute>() != null);
                
            foreach (var actionType in ruleActionTypes)
            {
                services.AddScoped(typeof(IRuleAction), actionType);
            }
        }
        
        // Register entity loaders
        services.Scan(scan => scan
            .FromAssemblies(pluginAssemblies)
            .AddClasses(classes => classes.AssignableTo<IEntityLoaderModule>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}
```

#### Manual Registration
For fine-grained control:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register specific plugins
    services.AddScoped<IRuleAction, EmailNotificationRuleAction>();
    services.AddScoped<IRuleAction, DataValidationRuleAction>();
    
    // Register with keyed service
    services.AddKeyedScoped<IRuleAction, CustomApprovalAction>("approval-v2");
    
    // Register plugin with factory
    services.AddScoped<IEntityLoaderModule>(provider =>
    {
        var config = provider.GetRequiredService<IConfiguration>();
        return new ConfigurableEntityLoader(config.GetSection("EntityLoader"));
    });
}
```

### Creating Custom Plugins

#### Custom Validation Plugin

```csharp
public interface ICustomValidator
{
    Task<ValidationResult> ValidateAsync(object entity, ValidationContext context);
}

[AttributeUsage(AttributeTargets.Class)]
public class CustomValidatorAttribute : Attribute
{
    public string ValidatorName { get; }
    public CustomValidatorAttribute(string name) => ValidatorName = name;
}

[CustomValidator("business-rules")]
public class BusinessRulesValidator : ICustomValidator
{
    private readonly IBusinessRulesEngine _rulesEngine;
    
    public async Task<ValidationResult> ValidateAsync(
        object entity, 
        ValidationContext context)
    {
        var violations = new List<string>();
        
        // Run business rules
        var results = await _rulesEngine.EvaluateAsync(entity, context);
        
        foreach (var rule in results.Where(r => !r.Passed))
        {
            violations.Add($"{rule.RuleName}: {rule.Message}");
        }
        
        return violations.Any() 
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}
```

#### Custom Repository Plugin

```csharp
public interface IRepositoryPlugin<T> where T : class, IDataModel
{
    Task OnBeforeCreateAsync(T entity, IPluginContext context);
    Task OnAfterCreateAsync(T entity, IPluginContext context);
    Task OnBeforeUpdateAsync(T entity, T original, IPluginContext context);
    Task OnAfterUpdateAsync(T entity, T original, IPluginContext context);
    Task OnBeforeDeleteAsync(T entity, IPluginContext context);
    Task OnAfterDeleteAsync(Guid entityId, IPluginContext context);
}

public class AuditRepositoryPlugin<T> : IRepositoryPlugin<T> 
    where T : class, IDataModel
{
    private readonly IAuditService _auditService;
    
    public async Task OnAfterCreateAsync(T entity, IPluginContext context)
    {
        await _auditService.LogAsync(new AuditEntry
        {
            EntityType = typeof(T).Name,
            EntityId = entity.Id,
            Action = "Create",
            UserId = context.UserId,
            Timestamp = DateTime.UtcNow,
            Data = JsonConvert.SerializeObject(entity)
        });
    }
    
    public async Task OnAfterUpdateAsync(T entity, T original, IPluginContext context)
    {
        var changes = GetChanges(original, entity);
        
        await _auditService.LogAsync(new AuditEntry
        {
            EntityType = typeof(T).Name,
            EntityId = entity.Id,
            Action = "Update",
            UserId = context.UserId,
            Timestamp = DateTime.UtcNow,
            Changes = changes
        });
    }
}
```

### Extension Points

#### Custom Form Field Types

```csharp
[FormFieldType("color-picker")]
public class ColorPickerFieldType : IFormFieldType
{
    public string TypeName => "color-picker";
    
    public ValidationResult Validate(
        FormFieldDefinition definition,
        object? value)
    {
        if (value == null && definition.Required)
            return ValidationResult.Failure("Color is required");
            
        if (value is string colorValue)
        {
            // Validate hex color format
            var regex = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            if (!Regex.IsMatch(colorValue, regex))
                return ValidationResult.Failure("Invalid color format");
        }
        
        return ValidationResult.Success();
    }
    
    public object? Transform(
        FormFieldDefinition definition,
        object? value)
    {
        // Normalize color format
        if (value is string color && color.StartsWith("#"))
        {
            return color.ToUpperInvariant();
        }
        return value;
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new()
        {
            ["defaultValue"] = "#000000",
            ["format"] = "hex",
            ["supportsAlpha"] = false
        };
    }
}
```

#### Custom Notification Channels

```csharp
public interface INotificationChannel
{
    string ChannelName { get; }
    Task<bool> CanSendAsync(NotificationMessage message);
    Task<NotificationResult> SendAsync(NotificationMessage message);
}

[NotificationChannel("teams")]
public class TeamsNotificationChannel : INotificationChannel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    
    public string ChannelName => "teams";
    
    public async Task<bool> CanSendAsync(NotificationMessage message)
    {
        // Check if recipient has Teams webhook
        return message.Metadata.ContainsKey("teamsWebhook");
    }
    
    public async Task<NotificationResult> SendAsync(NotificationMessage message)
    {
        var webhook = message.Metadata["teamsWebhook"];
        var client = _httpClientFactory.CreateClient();
        
        var teamsMessage = new
        {
            title = message.Subject,
            text = message.Body,
            themeColor = GetThemeColor(message.Priority),
            sections = BuildSections(message)
        };
        
        var response = await client.PostAsJsonAsync(webhook, teamsMessage);
        
        return response.IsSuccessStatusCode
            ? NotificationResult.Success(response.StatusCode.ToString())
            : NotificationResult.Failure($"Teams API error: {response.StatusCode}");
    }
}
```

### Plugin Lifecycle Management

```csharp
public interface IPluginLifecycle
{
    Task OnLoadAsync(IPluginContext context);
    Task OnStartAsync(IPluginContext context);
    Task OnStopAsync(IPluginContext context);
    Task OnUnloadAsync(IPluginContext context);
}

public class PluginManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, IPluginLifecycle> _plugins = new();
    
    public async Task LoadPluginAsync(Type pluginType)
    {
        var plugin = (IPluginLifecycle)_serviceProvider.GetRequiredService(pluginType);
        var pluginName = pluginType.Name;
        
        _plugins[pluginName] = plugin;
        
        var context = new PluginContext
        {
            PluginName = pluginName,
            Configuration = GetPluginConfiguration(pluginName)
        };
        
        await plugin.OnLoadAsync(context);
        await plugin.OnStartAsync(context);
    }
    
    public async Task UnloadPluginAsync(string pluginName)
    {
        if (_plugins.TryGetValue(pluginName, out var plugin))
        {
            var context = new PluginContext { PluginName = pluginName };
            
            await plugin.OnStopAsync(context);
            await plugin.OnUnloadAsync(context);
            
            _plugins.Remove(pluginName);
        }
    }
}
```

### Best Practices for Plugins

1. **Follow interface contracts** - Implement all required methods
2. **Handle errors gracefully** - Don't let plugins crash the system
3. **Use dependency injection** - Leverage DI for plugin dependencies
4. **Document plugin requirements** - Clear documentation for users
5. **Version your plugins** - Support multiple versions if needed
6. **Test plugin interactions** - Ensure plugins work together
7. **Provide configuration options** - Make plugins configurable
8. **Use async patterns** - Support async operations throughout
9. **Implement dispose patterns** - Clean up resources properly
10. **Monitor plugin performance** - Track plugin execution time

---

## Dynamic vs Application-Specific Data Patterns

BFormDomain supports two distinct approaches to data modeling: dynamic data structures for end-user defined content and application-specific structures for core business logic.

### Dynamic Data Structures

Dynamic data structures are designed for scenarios where end users need to define their own data schemas:

#### When to Use Dynamic Data
- User-defined forms with custom fields
- Configurable workflows and processes
- Ad-hoc reporting tables
- Customer-specific data extensions
- Multi-tenant applications with varied schemas

#### Dynamic Form Example

```csharp
// End user defines a form template
var customerForm = new FormTemplate
{
    Id = Guid.NewGuid(),
    Name = "customer-onboarding",
    Title = "Customer Onboarding Form",
    Fields = new List<FormFieldDefinition>
    {
        new()
        {
            Name = "companyName",
            Type = "text",
            Label = "Company Name",
            Required = true,
            Validation = new { maxLength = 100 }
        },
        new()
        {
            Name = "industry",
            Type = "select",
            Label = "Industry",
            Options = new[] { "Technology", "Healthcare", "Finance", "Other" }
        },
        new()
        {
            Name = "annualRevenue",
            Type = "number",
            Label = "Annual Revenue",
            Validation = new { min = 0, max = 1000000000 }
        }
    }
};

// User submits form data
var formInstance = new FormInstance
{
    Template = customerForm.Name,
    FieldData = new Dictionary<string, object>
    {
        ["companyName"] = "Acme Corp",
        ["industry"] = "Technology",
        ["annualRevenue"] = 5000000
    }
};
```

#### Dynamic Table Example

```csharp
// User defines a table structure
var inventoryTable = new TableTemplate
{
    Name = "inventory-tracking",
    Columns = new List<TableColumnDefinition>
    {
        new() { Name = "sku", Type = "string", Required = true },
        new() { Name = "productName", Type = "string" },
        new() { Name = "quantity", Type = "number" },
        new() { Name = "location", Type = "string" },
        new() { Name = "lastUpdated", Type = "datetime" }
    }
};

// User adds data
var tableLogic = new TableLogic(_tableRepo, _rowRepo);
await tableLogic.AddRowAsync(inventoryTable.Name, new TableRowData
{
    Data = new Dictionary<string, object>
    {
        ["sku"] = "PROD-001",
        ["productName"] = "Widget A",
        ["quantity"] = 150,
        ["location"] = "Warehouse B",
        ["lastUpdated"] = DateTime.UtcNow
    }
});
```

### Application-Specific Structures

Application-specific structures are strongly-typed domain models for core business logic:

#### When to Use Application-Specific
- Core business entities
- Complex domain logic
- Performance-critical operations
- Type safety requirements
- IDE support and refactoring

#### Application-Specific Entity Example

```csharp
// Strongly typed domain entity
public class Product : IAppEntity
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // IAppEntity implementation
    public string EntityType { get; set; } = nameof(Product);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
    public List<string> AttachedSchedules { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    
    // Domain-specific properties
    public string SKU { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public ProductCategory Category { get; set; }
    public List<ProductVariant> Variants { get; set; } = new();
    public InventoryInfo Inventory { get; set; }
    
    // Business logic
    public void AdjustPrice(decimal percentage)
    {
        Price = Price * (1 + percentage / 100);
        UpdatedDate = DateTime.UtcNow;
    }
    
    public bool IsInStock()
    {
        return Inventory?.QuantityOnHand > 0;
    }
}

// Repository using base infrastructure
public class ProductRepository : MongoRepository<Product>
{
    public ProductRepository(IDataEnvironment env, IApplicationAlert alerts) 
        : base(env, alerts)
    {
    }
    
    // Custom queries
    public async Task<List<Product>> GetByCategoryAsync(ProductCategory category)
    {
        var (products, _) = await GetAllAsync(p => p.Category == category);
        return products;
    }
    
    public async Task<List<Product>> GetLowStockProductsAsync(int threshold)
    {
        var (products, _) = await GetAllAsync(
            p => p.Inventory.QuantityOnHand < threshold);
        return products;
    }
}
```

### Hybrid Approach

Combining dynamic and application-specific patterns:

```csharp
// Core entity with dynamic extensions
public class Customer : IAppEntity
{
    // Strongly typed core properties
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public CustomerType Type { get; set; }
    public DateTime RegisteredDate { get; set; }
    
    // Dynamic custom fields
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    // Reference to dynamic form data
    public List<Guid> FormSubmissions { get; set; } = new();
}

// Service combining both approaches
public class CustomerService
{
    private readonly IRepository<Customer> _customerRepo;
    private readonly FormLogic _formLogic;
    private readonly TableLogic _tableLogic;
    
    public async Task OnboardCustomerAsync(
        Customer customer,
        Dictionary<string, object> onboardingData)
    {
        // Save core customer entity
        await _customerRepo.CreateAsync(customer);
        
        // Save dynamic onboarding form
        var formInstance = await _formLogic.CreateInstanceAsync(
            "customer-onboarding",
            onboardingData,
            customer.Id);
            
        // Update customer with form reference
        customer.FormSubmissions.Add(formInstance.Id);
        await _customerRepo.UpdateAsync(customer);
        
        // Add to dynamic reporting table
        await _tableLogic.AddRowAsync("customer-analytics", new
        {
            customerId = customer.Id,
            customerType = customer.Type.ToString(),
            onboardingDate = DateTime.UtcNow,
            customData = customer.CustomFields
        });
    }
}
```

### Decision Matrix

| Criteria | Dynamic Data | Application-Specific |
|----------|-------------|---------------------|
| Schema Flexibility | High - User definable | Low - Code changes required |
| Type Safety | Low - Runtime validation | High - Compile-time checks |
| Performance | Moderate - JSON parsing | High - Direct property access |
| Querying | Complex - JSON queries | Simple - LINQ/Native queries |
| Refactoring | Difficult - String references | Easy - IDE support |
| Business Logic | Limited - Generic rules | Rich - Domain methods |
| Validation | Runtime - JSON Schema | Compile-time + Runtime |
| Documentation | Generated from schema | Code as documentation |

### Best Practices

1. **Start with application-specific** - Default to strongly-typed entities
2. **Use dynamic for user content** - Let users define their own structures
3. **Keep core logic typed** - Business rules in domain entities
4. **Validate dynamic data thoroughly** - Use JSON Schema validation
5. **Index dynamic fields carefully** - Consider query performance
6. **Version dynamic schemas** - Track schema evolution
7. **Provide migration tools** - Help users update schemas
8. **Document the distinction** - Clear guidance for developers
9. **Monitor performance** - Dynamic queries can be slower
10. **Test both patterns** - Ensure they work well together

---