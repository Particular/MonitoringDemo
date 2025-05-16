using Messages;

namespace Shipping;

public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}