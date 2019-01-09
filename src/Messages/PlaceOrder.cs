namespace Messages
{
    using NServiceBus;

    public class PlaceOrder :
        ICommand
    {
        public string OrderId { get; set; }
    }
}