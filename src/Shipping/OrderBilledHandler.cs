namespace Shipping
{
    using System.Threading.Tasks;
    using NServiceBus;
    using static Messages.NestedOrdersMessages;

    public class OrderBilledHandler :
        IHandleMessages<OrderBilled>
    {
        public OrderBilledHandler(SimulationEffects simulationEffects)
        {
            this.simulationEffects = simulationEffects;
        }

        public Task Handle(OrderBilled message, IMessageHandlerContext context)
        {
            return simulationEffects.SimulateOrderBilledMessageProcessing();
        }

        SimulationEffects simulationEffects;
    }
}