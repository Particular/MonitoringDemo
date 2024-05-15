using Messages;

namespace Billing;

public class OrderPlacedHandler(SimulationEffects simulationEffects) : IHandleMessages<OrderPlaced>
{
    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        await simulationEffects.SimulatedMessageProcessing()
            .ConfigureAwait(false);

        var orderBilled = new OrderBilled
        {
            OrderId = message.OrderId
        };
        await context.Publish(orderBilled)
            .ConfigureAwait(false);
    }
}