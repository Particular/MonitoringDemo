namespace Shared;

public class ProcessingEndpointControls(Func<EndpointConfiguration> endpointConfigProvider)
{
    private IEndpointInstance? runningEndpoint;

    private bool delayedRetries;
    private bool autoThrottle;

    private readonly RetrievingMessageProgressBehavior retrievingMessageProgressBehavior = new RetrievingMessageProgressBehavior();
    private readonly ProcessingMessageProgressBehavior processingMessageProgressBehavior = new ProcessingMessageProgressBehavior();
    private readonly DispatchingProgressBehavior dispatchingMessageProgressBehavior = new DispatchingProgressBehavior();
    private readonly SlowProcessingSimulationBehavior slowProcessingSimulationBehavior = new SlowProcessingSimulationBehavior();
    private readonly DatabaseFailureSimulationBehavior databaseFailureSimulationBehavior = new DatabaseFailureSimulationBehavior();
    private readonly DatabaseDownSimulationBehavior databaseDownSimulationBehavior = new DatabaseDownSimulationBehavior();
    private CancellationTokenSource? stopTokenSource;
    private readonly SemaphoreSlim restartSemaphore = new SemaphoreSlim(1);
    private Task? restartTask;

    void Register(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.Pipeline.Register(retrievingMessageProgressBehavior, "Shows progress of retrieving messages");
        endpointConfiguration.Pipeline.Register(processingMessageProgressBehavior, "Shows progress of processing messages");
        endpointConfiguration.Pipeline.Register(dispatchingMessageProgressBehavior, "Shows progress of dispatching messages");
        endpointConfiguration.Pipeline.Register(slowProcessingSimulationBehavior, "Simulates slow processing");
        endpointConfiguration.Pipeline.Register(databaseFailureSimulationBehavior, "Simulates faulty database");
        endpointConfiguration.Pipeline.Register(databaseDownSimulationBehavior, "Simulates down database");
        endpointConfiguration.Pipeline.Register(new PropagateManualModeBehavior(), "Propagates manual mode settings");
    }

    public void Start()
    {
        stopTokenSource = new CancellationTokenSource();
        restartTask = Task.Run(async () =>
        {
            var stopToken = stopTokenSource.Token;
            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    await restartSemaphore.WaitAsync(stopToken);
                    //await Task.Delay(5000);
                    await RestartEndpoint();
                }
#pragma warning disable PS0019
                catch (Exception e)
#pragma warning restore PS0019
                {
                    Console.WriteLine(e);
                }
            }
        });
    }

#pragma warning disable PS0018
    async Task RestartEndpoint()
#pragma warning restore PS0018
    {
        if (runningEndpoint != null)
        {
            await runningEndpoint.Stop();
        }

        var config = endpointConfigProvider();

        if (!delayedRetries)
        {
            config.Recoverability().Delayed(settings => settings.NumberOfRetries(0));
        }

        if (autoThrottle)
        {
            var rateLimitSettings = new RateLimitSettings
            {
            };
            config.Recoverability().OnConsecutiveFailures(5, rateLimitSettings);
        }

        Register(config);

        runningEndpoint = await Endpoint.Start(config);
    }

#pragma warning disable PS0018
    public async Task StopEndpoint()
#pragma warning restore PS0018
    {
        stopTokenSource?.Cancel();
        if (restartTask != null)
        {
            await restartTask;
        }
        if (runningEndpoint != null)
        {
            await runningEndpoint.Stop();
        }
    }

    public void BindSlowProcessingDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial(
            'B', upKey, downKey,
            $"Press {upKey} to increase processing delay.{Environment.NewLine}Press {downKey} to increase it.",
            () => slowProcessingSimulationBehavior.ReportState(),
            x => slowProcessingSimulationBehavior.SetProcessingDelay(x));
    }

    public void BindDatabaseFailuresDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial(
            'C', upKey, downKey, $"Press {upKey} to increase database failure rate.{Environment.NewLine}Press {downKey} to decrease it.",
            () => databaseFailureSimulationBehavior.ReportState(),
            x => databaseFailureSimulationBehavior.SetFailureLevel(x));
    }

    public void BindDatabaseDownToggle(UserInterface userInterface, char toggleKey)
    {
        userInterface.BindToggle('D', toggleKey, $"Press {toggleKey} to toggle database down simulation.",
            () => databaseDownSimulationBehavior.ReportState(),
            () => databaseDownSimulationBehavior.Down(),
            () => databaseDownSimulationBehavior.Up());
    }

    public void BindDelayedRetriesToggle(UserInterface userInterface, char toggleKey)
    {
        userInterface.BindToggle('E', toggleKey, $"Press {toggleKey} to toggle delayed retries.",
            () => delayedRetries ? "Delayed retries enabled" : "Delayed retries disabled",
            () =>
            {
                delayedRetries = true;
                restartSemaphore.Release();
            },
            () =>
            {
                delayedRetries = false;
                restartSemaphore.Release();
            });
    }

    public void BindAutoThrottleToggle(UserInterface userInterface, char toggleKey)
    {
        userInterface.BindToggle('F', toggleKey, $"Press {toggleKey} to toggle auto throttle.",
            () => autoThrottle ? "Auto throttle enabled" : "Auto throttle disabled",
            () =>
            {
                autoThrottle = true;
                restartSemaphore.Release();
            },
            () =>
            {
                autoThrottle = false;
                restartSemaphore.Release();
            });
    }

    public void BindFailureReceivingButton(UserInterface userInterface, char key)
    {
        userInterface.BindButton('G', key, $"Press {key} to trigger failure while receiving a message",
            null,
            () => retrievingMessageProgressBehavior.Failure());
    }

    public void BindFailureProcessingButton(UserInterface userInterface, char key)
    {
        userInterface.BindButton('H', key, $"Press {key} to trigger failure while processing a message",
            null,
            () => processingMessageProgressBehavior.Failure());
    }

    public void BindFailureDispatchingButton(UserInterface userInterface, char key)
    {
        userInterface.BindButton('I', key, $"Press {key} to trigger failure while dispatching follow-up messages",
            null,
            () => dispatchingMessageProgressBehavior.Failure());
    }
}