namespace Billing;

using System;
using System.IO;
using System.Threading.Tasks;

public class SimulationEffects
{
    public void IncreaseFailureRate()
    {
        failureRate = Math.Min(1, failureRate + failureRateIncrement);
    }

    public void DecreaseFailureRate()
    {
        failureRate = Math.Max(0, failureRate - failureRateIncrement);
    }

    public void WriteState(TextWriter output)
    {
        output.WriteLine("Failure rate: {0:P0}", failureRate);
    }

    public async Task SimulatedMessageProcessing()
    {
        await Task.Delay(200)
            .ConfigureAwait(false);

        if (Random.Shared.NextDouble() < failureRate)
        {
            throw new Exception("BOOM! A failure occurred");
        }
    }

    double failureRate;
    const double failureRateIncrement = 0.1;
}