using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace Shipping
{
    public class OrderPlacedHandler :
        IHandleMessages<OrderPlaced>
    {
        private SimulationEffects simulationEffects;

        public OrderPlacedHandler(SimulationEffects simulationEffects)
        {
            this.simulationEffects = simulationEffects;
        }

        public Task Handle(OrderPlaced message, IMessageHandlerContext context)
        {
            return simulationEffects.SimulateOrderPlacedMessageProcessing();
        }
    }
}