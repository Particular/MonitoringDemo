using NServiceBus.Pipeline;
using Shared;

namespace Shared;

public class RetrievingMessageProgressBehavior : Behavior<ITransportReceiveContext>
{
    private FailureSimulator failureSimulator = new();

    public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
    {
        if (context.Message.Headers.ContainsKey("MonitoringDemo.ManualMode"))
        {
            await failureSimulator.RunInteractive($"Retrieving message {context.Message.MessageId}...", context.CancellationToken);
        }

        await next().ConfigureAwait(false);
    }

    public void Failure()
    {
        failureSimulator.Trigger();
    }
}