using NServiceBus.Pipeline;

namespace Shared;

public class PropagateManualModeBehavior : Behavior<IOutgoingLogicalMessageContext>
{
    public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
    {
        if (context.TryGetIncomingPhysicalMessage(out var incomingMessage)
            && incomingMessage.Headers.ContainsKey("MonitoringDemo.ManualMode"))
        {
            context.Headers["MonitoringDemo.ManualMode"] = "True";
        }
        return next();
    }
}