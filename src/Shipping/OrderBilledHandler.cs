namespace Shipping;

using System.Threading.Tasks;
using Messages;
using NServiceBus;

public class OrderBilledHandler(SimulationEffects simulationEffects) :
    IHandleMessages<OrderBilled>
{
    public Task Handle(OrderBilled message, IMessageHandlerContext context)
    {
        return simulationEffects.SimulateOrderBilledMessageProcessing();
    }
}