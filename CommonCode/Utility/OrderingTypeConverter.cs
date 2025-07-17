using BFormDomain.CommonCode.Platform.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Utility
{
    public class OrderingTypeConverter : JsonConverter<QueryOrdering>
    {
        public override QueryOrdering Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string tokenTypeString = reader.GetString()!;
                if (Enum.TryParse(tokenTypeString, out QueryOrdering tokenType))
                {
                    return tokenType;
                }
            }

            throw new JsonException("Invalid JSON token type");
        }

        public override void Write(Utf8JsonWriter writer, QueryOrdering value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
