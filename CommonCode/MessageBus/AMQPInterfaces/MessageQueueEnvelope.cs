using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace BFormDomain.MessageBus;

/// <summary>
/// MessageQueueEnvelope serializes messages to json and streams the serialized object to memory. 
///     -References:
///         > Not uses yet
///     -Functions:
///         >Decode:
/// </summary>
public class MessageQueueEnvelope
{
    private readonly JsonSerializer _serializer = new();

    public MessageQueueEnvelope()
    {
    }

    public MessageQueueEnvelope(object data)
    {
        data.Requires().IsNotNull();

        var ms = new MemoryStream();
        var bsw = new BsonDataWriter(ms);
        MessageType = data.GetType();
        _serializer.Serialize(bsw, data);
        Data = ms.ToArray();
    }

    public Type? MessageType { get; set; } 
    public byte[]? Data { get; set; }

    public object? Decode()
    {
        Data!.Requires().IsNotNull();
        MessageType!.Requires().IsNotNull();
        
        var ms = new MemoryStream(Data!);
        var bsr = new BsonDataReader(ms);
        bsr.Guarantees().IsNotNull();

        return _serializer.Deserialize(bsr, MessageType!);
    }
}
