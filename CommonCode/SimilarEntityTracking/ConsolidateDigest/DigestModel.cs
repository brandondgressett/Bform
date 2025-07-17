namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

public class DigestModel
{
    public List<DigestEntryModel> Entries { get; set; } = new List<DigestEntryModel>();

    public bool MaybeAppendEntry(DigestEntryModel entry, int limit)
    {
        bool appended;
        if(Entries.Count + 1 < limit)
        {
            Entries.Add(entry);
            appended = true;
        } else
        {
            appended = false;
        }

        return appended;
            
    }

    public void MaybeCircularAppendEntry(DigestEntryModel entry, int limit)
    {
        if(Entries.Count + 1 >= limit)
        {
            Entries.RemoveAt(0);  
        } 

        Entries.Add(entry);
    }

    public static DigestModel Merge(DigestModel a, DigestModel b)
    {
        return new DigestModel
        {
            Entries = a.Entries.Concat(b.Entries).ToList()
        };
    }

    public static void SpilloverAppend(DigestModel head, int headLimit, DigestModel tail, int tailLimit, DigestEntryModel entry)
    {
        if (!head.MaybeAppendEntry(entry, headLimit))
            tail.MaybeCircularAppendEntry(entry, tailLimit);
    }

}
