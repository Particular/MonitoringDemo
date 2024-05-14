namespace Shipping;

using System.Threading.Tasks;
using Messages;
using NServiceBus;

public class OrderPlacedHandler(SimulationEffects simulationEffects) :
    IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        return simulationEffects.SimulateOrderPlacedMessageProcessing();
    }
}