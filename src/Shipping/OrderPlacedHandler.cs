using Messages;

namespace Shipping;

public class OrderPlacedHandler(SimulationEffects simulationEffects) : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        return simulationEffects.SimulateOrderPlacedMessageProcessing();
    }
}