using Messages;
using Shared;

namespace ClientUI;

class SimulatedCustomers(IEndpointInstance endpointInstance)
{
    public void BindSendingRateDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial('B', upKey, downKey, 
            $"Press {upKey} to increase sending rate or {downKey} to decrease it.",
            () => $"Sending rate: {rate}", x => rate = x + 1); //Rate is from 1 to 10
    }

    public void BindDuplicateLikelihoodDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial('C', upKey, downKey, 
            $"Press {upKey} to increase duplicate message rate or {downKey} to decrease it.",
            () => $"Duplicate rate: {duplicateLikelihood * 10}%", x => duplicateLikelihood = x);
    }

    public void BindManualModeToggle(UserInterface userInterface, char toggleKey)
    {
        userInterface.BindToggle('D', toggleKey, $"Press {toggleKey} to toggle manual send mode",
            () => manualMode ? "Manual sending mode" : "Automatic sending mode",
            () => manualMode = true, () => manualMode = false);
    }

    public void BindManualSendButton(UserInterface userInterface, char key)
    {
        userInterface.BindButton('G', key, $"Press {key} to send a message", null, () => manualModeSemaphore.Release());
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

    async Task PlaceSingleOrder(CancellationToken cancellationToken)
    {
        var placeOrderCommand = new PlaceOrder
        {
            OrderId = Guid.NewGuid().ToString()
        };

        var sendOptions = await SendOneMessage(cancellationToken, placeOrderCommand);

        if (manualMode)
        {
            Console.WriteLine($"Message {sendOptions.GetMessageId()} sent.");
        }

        if (Random.Shared.Next(10) < duplicateLikelihood)
        {
            //Send a duplicate
            await SendOneMessage(cancellationToken, placeOrderCommand);

            if (manualMode)
            {
                Console.WriteLine($"Duplicate message {sendOptions.GetMessageId()} sent.");
            }
        }
    }

    private async Task<SendOptions> SendOneMessage(CancellationToken cancellationToken, PlaceOrder placeOrderCommand)
    {
        var sendOptions = new SendOptions();

        if (manualMode)
        {
            sendOptions.SetHeader("MonitoringDemo.ManualMode", "True");
        }

        sendOptions.SetHumanReadableMessageId();
        await endpointInstance.Send(placeOrderCommand, sendOptions, cancellationToken);
        return sendOptions;
    }

    DateTime nextReset;
    int currentIntervalCount;
    int rate = 1;
    private int duplicateLikelihood;
    private bool manualMode;
    private SemaphoreSlim manualModeSemaphore = new SemaphoreSlim(0);
}