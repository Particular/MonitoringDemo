using Messages;
using Microsoft.Extensions.Hosting;

namespace ClientUI;

class SimulatedCustomers(IMessageSession messageSession)
{
    public string State => $"{(highTrafficMode ? "High" : "Low")} traffic mode - sending {rate} orders / second";

    public void ToggleTrafficMode()
    {
        highTrafficMode = !highTrafficMode;
        rate = highTrafficMode ? 8 : 1;
    }

    public async Task Run(CancellationToken cancellationToken = default)
    {
        nextReset = DateTime.UtcNow.AddSeconds(1);
        currentIntervalCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            if (now > nextReset)
            {
                currentIntervalCount = 0;
                nextReset = now.AddSeconds(1);
            }

            await PlaceSingleOrder(cancellationToken);
            currentIntervalCount++;

            try
            {
                if (currentIntervalCount >= rate)
                {
                    var delay = nextReset - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    Task PlaceSingleOrder(CancellationToken cancellationToken)
    {
        var placeOrderCommand = new PlaceOrder
        {
            OrderId = Guid.NewGuid().ToString()
        };

        return messageSession.Send(placeOrderCommand, cancellationToken);
    }

    bool highTrafficMode;

    DateTime nextReset;
    int currentIntervalCount;
    int rate = 1;
}