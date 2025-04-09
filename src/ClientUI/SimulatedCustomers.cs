using Messages;

namespace ClientUI;

class SimulatedCustomers(IEndpointInstance endpointInstance)
{
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public void WriteState(TextWriter output)
    {
        var trafficMode = manualMode 
            ? "Manual sending mode" 
            : highTrafficMode ? $"High traffic mode - sending {rate} orders / second" : $"Low traffic mode - sending {rate} orders / second";
        output.WriteLine(trafficMode);
    }

    public void ToggleTrafficMode()
    {
        highTrafficMode = !highTrafficMode;
        rate = highTrafficMode ? 8 : 1;
    }

    public void ToggleManualMode()
    {
        manualMode = !manualMode;
    }

    public void SendManually()
    {
        manualModeSemaphore.Release();
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

            if (manualMode)
            {
                await manualModeSemaphore.WaitAsync();
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
        var messageId = new string(Enumerable.Range(0, 4).Select(x => Letters[Random.Shared.Next(Letters.Length)]).ToArray());

        var placeOrderCommand = new PlaceOrder
        {
            OrderId = Guid.NewGuid().ToString()
        };

        var sendOptions = new SendOptions();

        if (manualMode)
        {
            sendOptions.SetHeader("MonitoringDemo.SlowMotion", "True");
        }

        sendOptions.SetMessageId(messageId);
        return endpointInstance.Send(placeOrderCommand, sendOptions, cancellationToken);
    }

    bool highTrafficMode;

    DateTime nextReset;
    int currentIntervalCount;
    int rate = 1;
    private bool manualMode;
    private SemaphoreSlim manualModeSemaphore = new SemaphoreSlim(0);
}