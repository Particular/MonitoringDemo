namespace Shipping
{
    using System.Threading.Tasks;
    using Messages;
    using NServiceBus;

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