using NServiceBus.Pipeline;
using NServiceBus.Transport;

namespace Sales;

public class DispatchingProgressBehavior : Behavior<IBatchDispatchContext>
{
    private FailureSimulator failureSimulator = new();

    public override async Task Invoke(IBatchDispatchContext context, Func<Task> next)
    {
        var incomingMessage = context.Extensions.Get<IncomingMessage>();
        if (incomingMessage.Headers.ContainsKey("MonitoringDemo.SlowMotion"))
        {
            Console.WriteLine($"Dispatching outgoing messages {incomingMessage.MessageId}...");
            await failureSimulator.RunInteractive(context.CancellationToken);
        }

        await next().ConfigureAwait(false);
    }

    public void Failure()
    {
        failureSimulator.Trigger();
    }
}