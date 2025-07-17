using BFormDomain.Validation;

namespace BFormDomain.CommonCode.Utility;

public static class GuidEncoder
{
    public static string? Encode(string guidText)
    {
        Guid guid = new(guidText);
        return Encode(guid);
    }

    public static string? Encode(Guid guid)
    {
        return Base32Encoder.ToBase32String(guid.ToByteArray());

    }

    public static string Dashless(Guid guid)
    {
        return guid.ToString().Replace("-", "");
    }

    public static Guid Decode(string encoded)
    {

        byte[]? buffer = Base32Encoder.FromBase32String(encoded);
        buffer.Guarantees().IsNotNull();
        return new Guid(buffer!);
    }

}