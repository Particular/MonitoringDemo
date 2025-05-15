using NServiceBus.Pipeline;
using Shared;

namespace Sales;

public class RetrievingMessageProgressBehavior : Behavior<ITransportReceiveContext>
{
    private FailureSimulator failureSimulator = new();

    public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
    {
        if (context.Message.Headers.ContainsKey("MonitoringDemo.SlowMotion"))
        {
            Console.WriteLine($"Retrieving message {context.Message.MessageId}...");
            await failureSimulator.RunInteractive(context.CancellationToken);
        }

        await next().ConfigureAwait(false);
    }

    public void Failure()
    {
        failureSimulator.Trigger();
    }
}