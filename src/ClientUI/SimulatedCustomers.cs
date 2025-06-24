using Messages;
using Shared;

namespace ClientUI;

class SimulatedCustomers(IEndpointInstance endpointInstance)
{
    public void BindSendingRateDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial('B', upKey, downKey,
            $"Press {upKey} to increase sending rate.{Environment.NewLine}Press {downKey} to decrease it.",
            () => $"Sending rate: {rate}", x => rate = x + 1); //Rate is from 1 to 10
    }

    public void BindDuplicateLikelihoodDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial('C', upKey, downKey,
            $"Press {upKey} to increase duplicate message rate.{Environment.NewLine}Press {downKey} to decrease it.",
            () => $"Duplicate rate: {duplicateLikelihood * 10}%", x => duplicateLikelihood = x);
    }

    public void BindManualModeToggle(UserInterface userInterface, char toggleKey)
    {
        userInterface.BindToggle('D', toggleKey, $"Press {toggleKey} to toggle manual send mode",
            () => manualMode ? "Manual sending mode" : "Automatic sending mode",
            () => manualMode = true, () =>
            {
                manualMode = false;
                manualModeSemaphore.Release();
            });
    }

    public void BindNoiseToggle(UserInterface userInterface, char toggleKey)
    {
        userInterface.BindToggle('E', toggleKey, $"Press {toggleKey} to toggle random noise",
            () => enableRandomNoise ? "Random noise" : "No random noise",
            () => enableRandomNoise = true, () => enableRandomNoise = false);
    }

    public void BindBlackFridayToggle(UserInterface userInterface, char toggleKey)
    {
        userInterface.BindToggle('F', toggleKey, $"Press {toggleKey} to toggle Black Friday mode",
            () => blackFriday ? "Black Friday!" : "Business as usual",
            () => blackFriday = true, () => blackFriday = false);
    }

    public void BindManualSendButton(UserInterface userInterface, char key)
    {
        userInterface.BindButton('G', key, $"Press {key} to send a message", null, () => manualModeSemaphore.Release());
    }

    private int EffectiveRate => Math.Max(blackFriday ? 32 : NoiseModifiedRate, 0);
    private int NoiseModifiedRate => enableRandomNoise ? rate + noiseComponent : rate;

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

                var noiseIncrease = Random.Shared.Next(Math.Abs(noiseComponent) + 1) == 0;
                if (noiseComponent == 0)
                {
                    //Randomly go up or down
                }
                else if (noiseIncrease)
                {
                    noiseComponent += Math.Sign(noiseComponent);
                }
                else
                {
                    noiseComponent -= Math.Sign(noiseComponent);
                }

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
                if (currentIntervalCount >= EffectiveRate)
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

        var messageId = MessageIdHelper.GetHumanReadableMessageId();

        await SendOneMessage(messageId, cancellationToken, placeOrderCommand);

        if (manualMode)
        {
            Console.WriteLine($"Message {messageId} sent.");
        }

        if (Random.Shared.Next(10) < duplicateLikelihood)
        {
            //Send a duplicate
            await SendOneMessage(messageId, cancellationToken, placeOrderCommand);

            if (manualMode)
            {
                Console.WriteLine($"Duplicate message {messageId} sent.");
            }
        }
    }

    private async Task SendOneMessage(string messageId, CancellationToken cancellationToken, PlaceOrder placeOrderCommand)
    {
        var sendOptions = new SendOptions();

        if (manualMode)
        {
            sendOptions.SetHeader("MonitoringDemo.ManualMode", "True");
        }

        sendOptions.SetMessageId(messageId);
        await endpointInstance.Send(placeOrderCommand, sendOptions, cancellationToken);
    }

    DateTime nextReset;
    int currentIntervalCount;
    int rate = 1;
    private int noiseComponent = 0;
    private bool enableRandomNoise;
    private int duplicateLikelihood;
    private bool manualMode;
    private bool blackFriday;
    private SemaphoreSlim manualModeSemaphore = new SemaphoreSlim(0);
}