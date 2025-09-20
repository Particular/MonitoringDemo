using NServiceBus.Pipeline;

namespace Shared;

public class DatabaseDownSimulationBehavior : Behavior<IInvokeHandlerContext>
{
    private bool databaseDown;

    public override Task Invoke(IInvokeHandlerContext context, Func<Task> next)
    {
        if (databaseDown)
        {
            throw new Exception("Simulated");
        }
        return next();
    }

    public string ReportState()
    {
        return databaseDown ? "Database down" : "Database up";
    }

    public void Down()
    {
        databaseDown = true;
    }

    public void Up()
    {
        databaseDown = false;
    }
}