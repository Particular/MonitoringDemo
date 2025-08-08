using NServiceBus.Pipeline;

namespace Shared;

public class DatabaseFailureSimulationBehavior : Behavior<IInvokeHandlerContext>
{
    private int failureLevel = 0;

    public override Task Invoke(IInvokeHandlerContext context, Func<Task> next)
    {
        if (Random.Shared.Next(10) < failureLevel)
        {
            throw new Exception("Simulated");
        }
        return next();
    }

    public void SetFailureLevel(int randomFailureLevel)
    {
        failureLevel = randomFailureLevel;
    }

    public string ReportState()
    {
        return $"Likelihood of random database failure: {failureLevel * 10}%";
    }
}