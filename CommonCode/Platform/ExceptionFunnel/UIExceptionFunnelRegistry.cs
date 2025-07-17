using BFormDomain.Diagnostics;
using BFormDomain.Validation;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform;






/// <summary>
/// The UIExceptionFunnelRegistry class registers exceptions with funnels, translates the exceptions into more end-user-friendly UIError objects.
/// </summary>
public class UIExceptionFunnelRegistry
{
    /// <summary>
    /// App terms is where to store end-user-friendly messages for the exceptions to be translated to.
    /// </summary>
    private readonly IApplicationTerms _appTerms;

    /// <summary>
    /// _alerts raises alerts from anywhere in the app. It's a way to convey debug information.
    /// </summary>
    private readonly IApplicationAlert _alerts;

    /// <summary>
    /// _funnelCatalog contains the definitions of all the different funnels; each funnel is for a different domain of the app.
    /// </summary>
    private readonly ConcurrentDictionary<UIExceptionFunnelTypes, UIExceptionFunnel> _funnelCatalog = new();

    /// <summary>
    /// DI Constructor.
    /// Bind as singleton.
    /// </summary>
    /// <param name="terms">Dependency Injected.</param>
    /// <param name="alerts">Dependency Injected.</param>
    public UIExceptionFunnelRegistry(
        IApplicationTerms terms,
        IApplicationAlert alerts)
    {
        _appTerms = terms;
        _alerts = alerts;
        Initialize();
    }

    /// <summary>
    /// Initialize() performs default registrations of exceptions funnelled by BForm.
    /// You may extend registrations by making calls to Register() from your own client
    /// code.
    /// </summary>
    private void Initialize()
    {
        UIExceptionFunnel webApiFunnel = new(_appTerms,_alerts);
      
        _funnelCatalog[UIExceptionFunnelTypes.WebAPI] = webApiFunnel;

        // Mongo Repository Specific
        UIExceptionFunnel repositoryFunnel = new(_appTerms,_alerts);
        repositoryFunnel.Register(
            new UIErrorTemplate(
            "Someone changed this while you had it open. Re-apply your changes and save again.", 
            typeof(IsNotEqualToException),
            new List<string>() { "document was already changed" }));

        repositoryFunnel.Register(
            new UIErrorTemplate(
                "Internal Server Error. Please try again.",
                typeof(ArgumentNullException),
                new List<string>() { "destination is null" }));

        repositoryFunnel.Register(
            new UIErrorTemplate(
                "You tried to access an item that doesn't exist.",
                typeof(ArgumentOutOfRangeException),
                new List<string>() { "bufferSize is negative or zero" }));

        repositoryFunnel.Register(
            new UIErrorTemplate(
                "Unable to overwrite.",
                typeof(NotSupportedException),
                new List<string>() { "The current steam does not support reading", "destination does not support writing" }));

        repositoryFunnel.Register(
            new UIErrorTemplate(
                "Failed to upload file data.",
                typeof(ObjectDisposedException),
                new List<string>() { "Either the current steam or destination were closed before the CopyTo(Stream) method was called." }));

        repositoryFunnel.Register(
            new UIErrorTemplate(
                "Error occurred with the server try again",
                typeof(IOException),
                new List<string>() { "An I/O error occurred" }));

        repositoryFunnel.Register(
            new UIErrorTemplate(
                "No access",
                typeof(ArgumentException),
                new List<string>() { "A handle to the pending read operation is not available.", "The pending operation does not support reading." }));

        repositoryFunnel.Register(
            new UIErrorTemplate(
                "Database Error",
                typeof(MongoException),
                new List<string>() { "" }));

        // TODO: Add some more registrations here
        _funnelCatalog[UIExceptionFunnelTypes.Repository] = repositoryFunnel;

        UIExceptionFunnel validationFunnel = new(_appTerms, _alerts);

        validationFunnel.Register(
            new UIErrorTemplate(
                "No items in list",
                typeof(DoesNotHaveLengthException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "list has items",
                typeof(HasLengthException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "List is longer than or equal to the amount provided",
                typeof(IsLongerOrEqualException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "List is longer than expected",
                typeof(IsLongerThanException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "List is shorter than or equal to input",
                typeof(IsNotLongerOrEqualException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "List is too short",
                typeof(IsNotLongerThanException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "Item exists no space",
                typeof(IsNotNullException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "Not correct type",
                typeof(IsNotOfTypeException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "List too short",
                typeof(IsNotShorterException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "List is just too short",
                typeof(IsNotShorterOrEqualException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "Item doesn't have value",
                typeof(IsNullException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "Is correct type",
                typeof(IsOfTypeException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "item is shorter",
                typeof(IsShorterException),
                new List<string> { "" }));

        validationFunnel.Register(
            new UIErrorTemplate(
                "Item is shorter than or equal to other item",
                typeof(IsShorterOrEqualException),
                new List<string> { "" }));

        _funnelCatalog[UIExceptionFunnelTypes.Validation] = validationFunnel;

    }

    /// <summary>
    /// Combines multiple funnels; a funnel is a way to categorize exceptions by their purpose or domain.
    /// </summary>
    /// <param name="funnelTypes">An enum of the funnel types to be combined.</param>
    /// <returns>The combined funnel.</returns>
    public UIExceptionFunnel CreateCombinedFunnel(params UIExceptionFunnelTypes[] funnelTypes)
    {
        var retval = new UIExceptionFunnel(_appTerms, _alerts);
        foreach (var funnelType in funnelTypes)
            retval.Register(_funnelCatalog[funnelType]);
        return retval;
    }




}