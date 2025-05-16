using NServiceBus.Pipeline;

namespace Shared;

public class ProcessingMessageProgressBehavior : Behavior<IIncomingLogicalMessageContext>
{
    private FailureSimulator failureSimulator = new();

    public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        if (context.Headers.ContainsKey("MonitoringDemo.ManualMode"))
        {
            await failureSimulator.RunInteractive($"Processing message {context.MessageId}...", context.CancellationToken);
        }

        await next().ConfigureAwait(false);
    }

    public void Failure()
    {
        failureSimulator.Trigger();
    }
}