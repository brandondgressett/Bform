namespace BFormDomain.HelperClasses;

public static class TypeNameExtensions
{
    public static string GetFriendlyTypeName(this Type type)
    {
        string friendlyName = type.Name;
        if (type.IsGenericType)
        {
            int iBacktick = friendlyName.IndexOf('`');
            if (iBacktick > 0)
            {
                friendlyName = friendlyName.Remove(iBacktick);
            }
            friendlyName += "<";
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                string typeParamName = GetFriendlyTypeName(typeParameters[i]);
                friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
            }
            friendlyName += ">";
            friendlyName = type.Namespace + "." + friendlyName;
        }
        else
        {
            //friendlyName = type.FullName ?? "unknown";
            friendlyName = type.Name ?? "unknown";
        }

        return friendlyName.Replace('+', '.');
    }

    public static string GetFullFriendlyTypeName(this Type type)
    {
        string friendlyName = type.Name;
        if (type.IsGenericType)
        {
            int iBacktick = friendlyName.IndexOf('`');
            if (iBacktick > 0)
            {
                friendlyName = friendlyName.Remove(iBacktick);
            }
            friendlyName += "<";
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                string typeParamName = GetFullFriendlyTypeName(typeParameters[i]);
                friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
            }
            friendlyName += ">";
            friendlyName = type.Namespace + "." + friendlyName;
        }
        else
        {
            //friendlyName = type.FullName ?? "unknown";
            friendlyName = type.Name ?? "unknown";
        }

        return friendlyName.Replace('+', '.');
    }
}
