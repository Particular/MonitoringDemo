namespace Shipping
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

        public Task Handle(OrderPlaced message, IMessageHandlerContext context)
        {
            return simulationEffects.SimulateOrderPlacedMessageProcessing();
        }

        private SimulationEffects simulationEffects;
    }
}