using BFormDomain.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace BFormDomain.CommonCode.Utility;

/// <summary>
/// 
/// Extends the MS dependency injection system to allow choosing from
/// multiple implementations of an interface using a key.
/// 
/// Example:
/// 
///     // Multiple implementations
///     public class WebPostDataSender: ISendData { }
///     public class GoogleSheetsDataAppender: ISendData { } 
///     public class TextFileDataAppender: ISendData { }
///     
///     // Desired traits
///     public enum DataEndpoints { Public,PrivateOnline,PrivateLocal };
/// 
///     // In Program.cs:
///     services.AddSingleton<WebPostDataSender>();
///     services.AddSingleton<GoogleSheetsDataAppender>();
///     services.AddSingleton<TextFileDataAppnder>();
///     
///     // Those are all the implementations of the same interface. 
///     // But I want to use "DataEndpoints" to choose an implementation
///     // to use during dependency injection to each constructor that
///     // takes ISendData in. So:
///     services.AddTransient<KeyInject<string,ISendData>.ServiceResolver>(
///         KeyInject<string,ISendData>.Factory(
///                 (DataEndpoints.Public.EnumName(), typeof(WebPostDataSender)),
///                 (DataEndpoints.PrivateOnline.EnumName(), typeof(GoogleSheetsDataAppender)),
///                 (DataEndpoints.PrivateLocal.EnumName(), typeof(TextFileDataAppender))
///             ));
///     
///     // To consume:
///     public class Consumer
///     {
///         private readonly ISendData _privateSendData, _publicSendData;
///         
///         public Consumer(KeyInject<string,ISendData>.ServiceResolver sendFactory)
///         {
///             _privateSendData = sendFactory(DataEndpoints.PrivateLocal.EnumName());
///             _publicSendData = sendFactory(DataEndpoints.Public.EnumName());
///         }
///      }
///             
/// 
/// 
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TInterface"></typeparam>
public static class KeyInject<TKey,TInterface> 
    where TInterface : class
    where TKey: IEquatable<TKey>
{
    // holy xml comments, Batman. The code is short compared to the doc. 


    public delegate TInterface ServiceResolver(TKey serviceType);
    
    public static Func<IServiceProvider,ServiceResolver> Factory(params (TKey, Type)[] mapping)
    {
        return provider => (ServiceResolver)(
            (TKey serviceType) =>
            {
                var match = mapping.First(x => x.Item1.Equals(serviceType));
                var svc = provider.GetService(match.Item2);
                var retval = svc as TInterface;
                retval.Guarantees().IsNotNull();
                return retval!;
            });
    }

}
