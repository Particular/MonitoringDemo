using Messages;

namespace Sales;

public class PlaceOrderHandler(SimulationEffects simulationEffects) : IHandleMessages<PlaceOrder>
{
    public async Task Handle(PlaceOrder message, IMessageHandlerContext context)
    {
        // Simulate the time taken to process a message
        await simulationEffects.SimulateMessageProcessing(context.CancellationToken);

        var orderPlaced = new OrderPlaced
        {
            OrderId = message.OrderId
        };

        var publishOptions = new PublishOptions();

        if (context.MessageHeaders.ContainsKey("MonitoringDemo.SlowMotion"))
        {
            publishOptions.SetHeader("MonitoringDemo.SlowMotion", "True");
        }

        await context.Publish(orderPlaced, publishOptions);
    }
}