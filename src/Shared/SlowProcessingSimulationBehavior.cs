using NServiceBus.Pipeline;

namespace Shared;

public class SlowProcessingSimulationBehavior : Behavior<IInvokeHandlerContext>
{
    readonly int baseProcessingTime = 1000;
    readonly int increment = 100;
    private int delayLevel = 0;

    public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
    {
        await Task.Delay(Delay, context.CancellationToken);
        await next().ConfigureAwait(false);
    }

    private TimeSpan Delay => TimeSpan.FromMilliseconds(baseProcessingTime + delayLevel * increment);

    public void SetProcessingDelay(int delayLevel)
    {
        this.delayLevel = delayLevel;
    }

    public string ReportState()
    {
        return $"Time to process each message: {Delay.TotalSeconds} seconds";
    }
}