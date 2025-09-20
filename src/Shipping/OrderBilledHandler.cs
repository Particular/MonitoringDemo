using Messages;

namespace Shipping;

public class OrderBilledHandler : IHandleMessages<OrderBilled>
{
    public Task Handle(OrderBilled message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}