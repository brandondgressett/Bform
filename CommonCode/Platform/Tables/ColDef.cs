using BFormDomain.CommonCode.Utility;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace BFormDomain.CommonCode.Platform.Tables;

public class ColDef
{
    public string Field { get; set; } = null!;
    public string HeaderName { get; set; } = null!;

    [JsonConverter(typeof(JTokenTypeConverter))]
    public JTokenType Type { get; set; }

    [JsonConverter(typeof(KeyTypeConverter))]
    public KeyType KeyType { get; set; }
   
}
