namespace Billing
{
    using System.Threading.Tasks;
    using Messages;
    using NServiceBus;

    public class OrderPlacedHandler :
        IHandleMessages<OrderPlaced>
    {
        public OrderPlacedHandler(SimulationEffects simulationEffects)
        {
            this.simulationEffects = simulationEffects;
        }

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

        SimulationEffects simulationEffects;
    }
}