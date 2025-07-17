using System;
using System.Collections.Generic;

namespace BFormDomain.MessageBus
{
    /// <summary>
    /// Generic typed message envelope for strongly-typed message handling
    /// </summary>
    /// <typeparam name="T">The type of message contained in the envelope</typeparam>
    public class MessageQueueEnvelope<T>
    {
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string QueueName { get; set; } = string.Empty;
        public T? Message { get; set; }
        public MessageContextInfo? MessageContext { get; set; }
        public IMessageAcknowledge? MessageAcknowledge { get; set; }
    }

    /// <summary>
    /// Context information about a message
    /// </summary>
    public class MessageContextInfo
    {
        public string MessageId { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public ulong DeliveryTag { get; set; }
        public bool Redelivered { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string RoutingKey { get; set; } = string.Empty;
    }
}