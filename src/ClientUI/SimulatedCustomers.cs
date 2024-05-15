using Messages;

namespace ClientUI;

class SimulatedCustomers(IEndpointInstance endpointInstance)
{
    public void WriteState(TextWriter output)
    {
        var trafficMode = highTrafficMode ? "High" : "Low";
        output.WriteLine($"{trafficMode} traffic mode - sending {rate} orders / second");
    }

    public void ToggleTrafficMode()
    {
        highTrafficMode = !highTrafficMode;
        rate = highTrafficMode ? 8 : 1;
    }

    public async Task Run(CancellationToken token)
    {
        nextReset = DateTime.UtcNow.AddSeconds(1);
        currentIntervalCount = 0;

        while (!token.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            if (now > nextReset)
            {
                currentIntervalCount = 0;
                nextReset = now.AddSeconds(1);
            }

            await PlaceSingleOrder();
            currentIntervalCount++;

            try
            {
                if (currentIntervalCount >= rate)
                {
                    var delay = nextReset - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, token);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    Task PlaceSingleOrder()
    {
        var placeOrderCommand = new PlaceOrder
        {
            OrderId = Guid.NewGuid().ToString()
        };

        return endpointInstance.Send(placeOrderCommand);
    }

    bool highTrafficMode;

    DateTime nextReset;
    int currentIntervalCount;
    int rate = 1;
}