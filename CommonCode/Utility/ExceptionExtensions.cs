using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace BFormDomain.HelperClasses;

public static class ExceptionExtensions
{
    private const string Line = "==============================================================================";


   

    public static string TraceInformation(this Exception exception,
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
    {
        if (exception == null)
        {
            return string.Empty;
        }

        var exceptionInformation = new StringBuilder();
        try
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            exceptionInformation.AppendLine($"{processName}:{DateTime.UtcNow}: Exception processed in: {memberName}:{sourceFilePath} line {sourceLineNumber}");

            exceptionInformation.Append(BuildMessage(exception));

            List<Exception> innerExceptions = new();

            Exception? innerException = exception.InnerException;

            if (innerException is not null)
            {
                innerExceptions.Add(innerException);
            }

            ReflectionTypeLoadException? reflectionTypeLoadException = exception as ReflectionTypeLoadException;
            if (reflectionTypeLoadException is not null)
            {
                foreach (var loaderException in reflectionTypeLoadException.LoaderExceptions)
                {
                    innerExceptions.Add(loaderException!);
                }
            }

            var aggregateException = exception as AggregateException;
            if (aggregateException is not null)
            {
                innerExceptions.AddRange(aggregateException.Flatten().InnerExceptions);
            }

            foreach (var anInnerException in innerExceptions)
            {
                var anException = anInnerException;
                while (anException != null)
                {
                    exceptionInformation.AppendLine();
                    exceptionInformation.AppendLine();
                    exceptionInformation.Append(BuildMessage(anException));
                    anException = anException.InnerException;
                }
            }

            
            

        }
        catch (Exception unexpectedEx)
        {
            Debug.WriteLine(unexpectedEx.ToString());
        }

        return exceptionInformation.ToString();
    }

    private static string BuildMessage(Exception exception)
    {
        string? st = string.Empty;
        try
        {
            st = exception.StackTrace;
        }
        catch (Exception)
        {


        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}{1}{2}:{3}{4}{5}{6}{7}",
            Line,
            Environment.NewLine,
            exception.GetType().GetFriendlyTypeName(),
            exception.Message,
            Environment.NewLine,
            st??string.Empty,
            Environment.NewLine,
            Line);
    }
}
