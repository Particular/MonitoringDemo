namespace Messages
{
    using NServiceBus;
    public class NestedOrdersMessages
    {
        public class OrderBilled :
            IEvent
        {
            public string OrderId { get; set; }
        }
    }
}