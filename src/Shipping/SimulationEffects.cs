namespace Shipping;

public class SimulationEffects
{
    public void WriteState(TextWriter output)
    {
        output.WriteLine("Base time to handle each OrderBilled event: {0} seconds", baseProcessingTime.TotalSeconds);

        output.Write("Simulated degrading resource: ");
        output.WriteLine(degradingResourceSimulationStarted.HasValue ? "ON" : "OFF");
    }

    public Task SimulateOrderBilledMessageProcessing()
    {
        return Task.Delay(baseProcessingTime);
    }

    public void ProcessMessagesFaster()
    {
        if (baseProcessingTime > TimeSpan.Zero)
        {
            baseProcessingTime -= increment;
        }
    }

    public void ProcessMessagesSlower()
    {
        baseProcessingTime += increment;
    }

    public Task SimulateOrderPlacedMessageProcessing()
    {
        var delay = TimeSpan.FromMilliseconds(200) + Degradation();
        return Task.Delay(delay);
    }

    public void ToggleDegradationSimulation()
    {
        degradingResourceSimulationStarted = degradingResourceSimulationStarted.HasValue ? default(DateTime?) : DateTime.UtcNow;
    }

    TimeSpan Degradation()
    {
        var timeSinceDegradationStarted = DateTime.UtcNow - (degradingResourceSimulationStarted ?? DateTime.MaxValue);
        if (timeSinceDegradationStarted < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return new TimeSpan(timeSinceDegradationStarted.Ticks / degradationRate);
    }

    TimeSpan baseProcessingTime = TimeSpan.FromMilliseconds(700);
    TimeSpan increment = TimeSpan.FromMilliseconds(100);

    DateTime? degradingResourceSimulationStarted;
    const int degradationRate = 5;
}