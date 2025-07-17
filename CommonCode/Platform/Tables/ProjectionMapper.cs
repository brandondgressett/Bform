using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Tables;

/// <summary>
/// ProjectionMapper maps table template information into a JSON object repesenting table row data 
///     -References:
///         >TableLogic
///     -Functions:
///         >Project
///         >MapPropertyIntoProjection
///         >ProcessRoundNumeric
///         >ProcessBinNumeric
///         >ProcessDateTruncation
///         >ProcessEntityReference
///         >ProcessDataCopy
/// </summary>
public class ProjectionMapper
{
    public static JObject Project(JObject sourceData, TableTemplate template, Dictionary<string, Mapping> maps)
    {
        var projection = new JObject();

        foreach (var col in template.Columns)
        {
            maps.ContainsKey(col.Field).Guarantees().IsTrue();
            var map = maps[col.Field];

            var property = sourceData.SelectToken(map.Query);
            if (property is not null)
            {
                MapPropertyIntoProjection(projection, col, map, property);
            }
            else
            {
                map.Nullable.Guarantees($"Table row mapping: {col.Field} isn't nullable, but query {map.Query} didn't find a value.").IsTrue();
                projection.Add(col.Field, null);
                
            }
        }

        return projection;

    }


    private static void MapPropertyIntoProjection(JObject projection, ColDef col, Mapping map, JToken property)
    {
        if (map.MakeEntityReference && !string.IsNullOrWhiteSpace(map.EntityDomain))
        {
            ProcessEntityReference(projection, col, map, property);
        }
        else if (map.TruncateDate)
        {
            ProcessDateTruncation(projection, col, map, property);
        }
        else if (map.RoundNumeric)
        {
            ProcessRoundNumeric(projection, col, map, property);
        }
        else if (map.BinNumeric)
        {
            ProcessBinNumeric(projection, col, map, property);
        }
        else // default case, typical field 
        {
            ProcessDataCopy(projection, col, map, property);
        }

        
    }

    private static void ProcessRoundNumeric(JObject projection, ColDef col, Mapping map, JToken property)
    {
        double ppnum = 0.0;
        try
        {
            ppnum = (double)property;
            map.SignificantDigits.Guarantees().IsNotGreaterOrEqual(0);
            ppnum = Math.Round(ppnum, map.SignificantDigits);
            projection.Add(col.Field, ppnum);
        }
        catch
        {
            false.Guarantees($"Table row mapping: {col.Field} requires a double field.").IsTrue();
        }
    }

    private static void ProcessBinNumeric(JObject projection, ColDef col, Mapping map, JToken property)
    {
        double ppnum = 0.0;
        try
        {
            ppnum = (double)property;
            map.BinNumericList.Guarantees().IsNotNull();
            var bin = map.BinNumericList!.First(b => ppnum >= b.Min && ppnum < b.Max);
            projection.Add(col.Field, bin.Name);
        }
        catch
        {
            false.Guarantees($"Table row mapping: {col.Field} requires a double field with a valid numeric bin.").IsTrue();
        }
    }

    private static void ProcessDateTruncation(JObject projection, ColDef col, Mapping map, JToken property)
    {
        DateTime ppdt = DateTime.MinValue;
        try
        {
            ppdt = (DateTime)property;
            ppdt = ppdt.Truncate(map.TruncationPeriod);
            projection.Add(col.Field, ppdt);
        }
        catch
        {
            ppdt.Guarantees($"Table row mapping: {col.Field} requires a Date field.").IsNotEqualTo(DateTime.MinValue);
        }
    }

    private static void ProcessEntityReference(JObject projection, ColDef col, Mapping map, JToken property)
    {
        throw new NotImplementedException(); // TODO: do it later
#if DOITLATER
        // property should be a guid id of an entity to reference.
        Guid refId = Guid.Empty;
        try
        {
            refId = (Guid)property;
            var builder = new UriBuilder("bform", map.EntityDomain, 0, refId.ToString()); // TODO: build better uri
            projection.Add(col.Field, builder.Uri.ToString());
        }
        catch
        {
            refId.Guarantees($"Table row mapping: {col.Field} requires the Guid id of a referenced entity.").IsNotEqualTo(Guid.Empty);
        }
#endif
    }

    private static void ProcessDataCopy(JObject projection, ColDef col, Mapping map, JToken property)
    {
        // assure types match
        property.Type.Guarantees($"Table row mapping: {col.Field} requires type {col.Type.EnumName()}, but query {map.Query} has selected a token of type {property.Type.EnumName()}").IsEqualTo(col.Type);

        // add property data into projection.
        projection.Add(col.Field, property);
    }
}
