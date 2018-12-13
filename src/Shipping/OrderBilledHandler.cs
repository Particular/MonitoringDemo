using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace Shipping
{
    public class OrderBilledHandler :
        IHandleMessages<OrderBilled>
    {
        SimulationEffects simulationEffects;

        public OrderBilledHandler(SimulationEffects simulationEffects)
        {
            this.simulationEffects = simulationEffects;
        }

        public Task Handle(OrderBilled message, IMessageHandlerContext context)
        {
            return simulationEffects.SimulateOrderBilledMessageProcessing();
        }
    }
}