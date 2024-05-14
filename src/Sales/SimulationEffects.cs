namespace Sales;

using System;
using System.IO;
using System.Threading.Tasks;

public class SimulationEffects
{
    public void WriteState(TextWriter output)
    {
        output.WriteLine("Base time to handle each order: {0} seconds", baseProcessingTime.TotalSeconds);
    }

    public Task SimulateMessageProcessing()
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

    TimeSpan baseProcessingTime = TimeSpan.FromMilliseconds(1300);
    TimeSpan increment = TimeSpan.FromMilliseconds(100);
}