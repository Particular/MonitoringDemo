namespace Shared;

public class FailureSimulator
{
    private bool failureTriggered = false;

#pragma warning disable PS0003
    public async Task RunInteractive(string taskDescription, CancellationToken cancellationToken)
#pragma warning restore PS0003
    {
        using var progressBar = new ProgressBar(taskDescription);

        for (var i = 0; i <= 100; i++)
        {
            if (failureTriggered)
            {
                failureTriggered = false;
                throw new Exception("Simulated failure");
            }
            progressBar.Update(i);
            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Trigger()
    {
        //TODO: Use Interlocked
        failureTriggered = true;
    }
}