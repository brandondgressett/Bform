namespace BFormDomain.MessageBus.InMemory;

public class LightMessageQueueEnvelope
{
    public LightMessageQueueEnvelope(object data)
    {
        Data = data;
        MessageType = data.GetType();
    }

    public Type MessageType { get; set; }
    public object Data { get; set; }

    public object Decode()
    {
        return Data;
    }
}
