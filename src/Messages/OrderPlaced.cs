namespace Some.Very.Long.Namespace.Is.found.here.Messages
{
    using NServiceBus;

    public class OrderPlaced :
        IEvent
    {
        public string OrderId { get; set; }
    }
}