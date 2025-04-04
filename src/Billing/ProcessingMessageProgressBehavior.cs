using NServiceBus.Pipeline;

namespace Billing;

public class ProcessingMessageProgressBehavior : Behavior<IIncomingLogicalMessageContext>
{
    private FailureSimulator failureSimulator = new();

    public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        if (context.Headers.ContainsKey("MonitoringDemo.SlowMotion"))
        {
            Console.WriteLine($"Processing message {context.MessageId}...");
            await failureSimulator.RunInteractive(context.CancellationToken);
        }

        await next().ConfigureAwait(false);
    }

    public void Failure()
    {
        failureSimulator.Trigger();
    }
}