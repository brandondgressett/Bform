namespace BFormDomain.MessageBus;

public record MessageContext<T>(T? Item, IMessageAcknowledge? Ack);


