namespace Billing;

using System.Threading.Tasks;
using Messages;
using NServiceBus;

public class OrderPlacedHandler(SimulationEffects simulationEffects) :
    IHandleMessages<OrderPlaced>
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