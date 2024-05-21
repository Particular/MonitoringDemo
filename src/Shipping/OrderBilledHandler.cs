using Messages;

namespace Shipping;

public class OrderBilledHandler(SimulationEffects simulationEffects) : IHandleMessages<OrderBilled>
{
    public Task Handle(OrderBilled message, IMessageHandlerContext context)
    {
        return simulationEffects.SimulateOrderBilledMessageProcessing(context.CancellationToken);
    }
}