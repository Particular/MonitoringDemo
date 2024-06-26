﻿using Messages;

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

        await context.Publish(orderPlaced);
    }
}