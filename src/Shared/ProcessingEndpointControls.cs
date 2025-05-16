namespace Shared;

public class ProcessingEndpointControls
{
    private readonly RetrievingMessageProgressBehavior retrievingMessageProgressBehavior = new RetrievingMessageProgressBehavior();
    private readonly ProcessingMessageProgressBehavior processingMessageProgressBehavior = new ProcessingMessageProgressBehavior();
    private readonly DispatchingProgressBehavior dispatchingMessageProgressBehavior = new DispatchingProgressBehavior();
    private readonly SlowProcessingSimulationBehavior slowProcessingSimulationBehavior = new SlowProcessingSimulationBehavior();
    private readonly DatabaseFailureSimulationBehavior databaseFailureSimulationBehavior = new DatabaseFailureSimulationBehavior();

    public void Register(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.Pipeline.Register(retrievingMessageProgressBehavior, "Shows progress of retrieving messages");
        endpointConfiguration.Pipeline.Register(processingMessageProgressBehavior, "Shows progress of processing messages");
        endpointConfiguration.Pipeline.Register(dispatchingMessageProgressBehavior, "Shows progress of dispatching messages");
        endpointConfiguration.Pipeline.Register(slowProcessingSimulationBehavior, "Simulates slow processing");
        endpointConfiguration.Pipeline.Register(databaseFailureSimulationBehavior, "Simulates faulty database");
        endpointConfiguration.Pipeline.Register(new PropagateManualModeBehavior(), "Propagates manual mode settings");
    }

    public void BindSlowProcessingDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial(
            'B', upKey, downKey, $"Press {upKey} to increase processing delay or {downKey} to decrease it.",
            () => slowProcessingSimulationBehavior.ReportState(),
            x => slowProcessingSimulationBehavior.SetProcessingDelay(x));
    }

    public void BindDatabaseFailuresDial(UserInterface userInterface, char upKey, char downKey)
    {
        userInterface.BindDial(
            'C', upKey, downKey, $"Press {upKey} to increase database failure rate or {downKey} to decrease it.",
            () => databaseFailureSimulationBehavior.ReportState(),
            x => databaseFailureSimulationBehavior.SetFailureLevel(x));
    }

    public void BindDatabaseDownToggle(UserInterface userInterface, char toggleKey)
    {

    }

    public void BindOutboxToggle(UserInterface userInterface, char toggleKey)
    {

    }

    public void BindAutoThrottleToggle(UserInterface userInterface, char toggleKey)
    {

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