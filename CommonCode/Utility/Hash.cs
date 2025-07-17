namespace BFormDomain.HelperClasses;

public static class Hash
{
    public static UInt64 KnuthHash(string read)
    {
        UInt64 hashedValue = 3074457345618258791ul;
        for (int i = 0; i < read.Length; i++)
        {
            hashedValue += read[i];
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }

    public static int GetCombinedHashCodeForValCollection<T>(IEnumerable<T> inputs)
    {
        
        inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        return GetCombinedHashCodeForHashesNested(inputs.Select(h => h!.GetHashCode()));
    }

    public static int GetCombinedHashCodeForCollection<T>(IEnumerable<T> inputs) where T : class
    {
        
        inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        return GetCombinedHashCodeForHashesNested(inputs.Select(h => null == h ? 1 : h.GetHashCode()));
    }

    public static int GetCombinedHashCodeForHashesNested(IEnumerable<int> inputs)
    {
        int hash = 17;
        inputs.ForEach(i => hash = hash*31 + i.GetHashCode());
        return hash;
    }

    public static int GetCombinedHashCode<T>(params T[] inputs) where T : class
    {
        return GetCombinedHashCodeForCollection(inputs);
    }

    public static int GetCombinedHashCodeForHashes(params int[] inputs)
    {
        return GetCombinedHashCodeForHashesNested(inputs);
    }
}
