using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Utility
{
    public class BetterBsonSerializer
    {
        public static string SerializeObject(object value)
        {
            try
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var writer = new BsonDataWriter(ms))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(writer, value);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error occurred during serialization: {ex.Message}");
                // You may choose to throw the exception or return a default value or null
                throw;
            }
        }

        public static T DeserializeObject<T>(string bson)
        {
            try
            {
                byte[] data = Convert.FromBase64String(bson);
                using (var ms = new System.IO.MemoryStream(data))
                {
                    using (var reader = new BsonDataReader(ms))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        return serializer.Deserialize<T>(reader)!;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error occurred during deserialization: {ex.Message}");
                // You may choose to throw the exception or return a default value or null
                throw;
            }
        }
    }
}
