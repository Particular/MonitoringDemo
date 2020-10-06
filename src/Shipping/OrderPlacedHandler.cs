namespace Shipping
{
    using System.Threading.Tasks;
    using NServiceBus;
    using Some.Very.Long.Namespace.Is.found.here.Messages;

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