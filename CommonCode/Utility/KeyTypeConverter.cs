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
    public class KeyTypeConverter : JsonConverter<KeyType>
    {
        public override KeyType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string keyTypeString = reader.GetString()!;
                if (Enum.TryParse(keyTypeString, out KeyType keyType))
                {
                    return keyType;
                }
            }

            throw new JsonException("Invalid KeyType");
        }

        public override void Write(Utf8JsonWriter writer, KeyType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
