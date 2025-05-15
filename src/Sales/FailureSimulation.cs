namespace Sales;

public class FailureSimulation
{
    private RetrievingMessageProgressBehavior retrievingMessageProgressBehavior = new RetrievingMessageProgressBehavior();
    private ProcessingMessageProgressBehavior processingMessageProgressBehavior = new ProcessingMessageProgressBehavior();
    private DispatchingProgressBehavior dispatchingMessageProgressBehavior = new DispatchingProgressBehavior();

    public void Register(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.Pipeline.Register(retrievingMessageProgressBehavior, "Shows progress of retrieving messages");

        endpointConfiguration.Pipeline.Register(processingMessageProgressBehavior, "Shows progress of processing messages");

        endpointConfiguration.Pipeline.Register(dispatchingMessageProgressBehavior, "Shows progress of dispatching messages");
    }

    public void TriggerFailureReceiving()
    {
        retrievingMessageProgressBehavior.Failure();
    }

    public void TriggerFailureProcessing()
    {
        processingMessageProgressBehavior.Failure();
    }

    public void TriggerFailureDispatching()
    {
        dispatchingMessageProgressBehavior.Failure();
    }
}