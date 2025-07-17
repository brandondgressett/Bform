namespace BFormDomain.MessageBus;

public class MessageQueueDeclaration
{
    public MessageQueueDeclaration()
    {
        Bindings = new List<string>();
    }


    public string Name { get; set; } = "";

    public List<string> Bindings { get; set; }
}
