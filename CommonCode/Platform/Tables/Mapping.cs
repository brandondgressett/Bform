using BFormDomain.CommonCode.Utility;

namespace BFormDomain.CommonCode.Platform.Tables;

public class Mapping
{
    
    public string Field { get; set; } = null!;
    
    /// <summary>
    /// Keep in mind that you can query the same source multiple times
    /// to repeat the column and use preprocessing
    /// </summary>
    public string Query { get; set; } = null!;


    public bool Nullable { get; set; } = false;


    #region field preprocessing

    public bool MakeEntityReference { get; set; }
    public string? EntityDomain { get; set; }

    public bool TruncateDate { get; set; }
    public DateTruncation TruncationPeriod { get; set; }

    public bool BinNumeric { get; set; }
    public List<NumericBin>? BinNumericList { get; set; }

    public bool RoundNumeric { get; set; }
    public int SignificantDigits { get; set; }
    #endregion
}
