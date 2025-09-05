using Messages;
using Shared;

namespace Billing;

public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        var orderBilled = new OrderBilled
        {
            OrderId = message.OrderId
        };

        var publishOptions = new PublishOptions();
        publishOptions.SetMessageId(MessageIdHelper.GetHumanReadableMessageId());
        await context.Publish(orderBilled, publishOptions);
    }
}