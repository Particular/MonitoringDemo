using MonitoringDemo;

CancellationTokenSource tokenSource = new();
Console.Title = "MonitoringDemo";

var remoteControlMode = args.Length > 0 && string.Equals(args[0], bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
var syncEvent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    tokenSource.Cancel();
    syncEvent.TrySetResult(true);
};

try
{
    using var launcher = new DemoLauncher(remoteControlMode);
    Console.WriteLine("Starting the Particular Platform");

    launcher.Platform();

    using (ColoredConsole.Use(ConsoleColor.Yellow))
    {
        Console.WriteLine(
            "Once ServiceControl has finished starting a browser window will pop up showing the ServicePulse monitoring tab");
    }

    Console.WriteLine("Starting Demo Solution");

    if (!tokenSource.IsCancellationRequested)
    {
        Console.WriteLine("Starting Billing endpoint.");
        launcher.Billing();

        Console.WriteLine("Starting Sales endpoint.");
        launcher.ScaleOutSales();

        Console.WriteLine("Starting Shipping endpoint.");
        launcher.Shipping();

        Console.WriteLine("Starting ClientUI endpoint.");
        launcher.ClientUI();

        using (ColoredConsole.Use(ConsoleColor.Yellow))
        {
            ScaleSalesEndpointIfRequired(launcher, syncEvent);

            await syncEvent.Task;

            Console.WriteLine("Shutting down");
        }
    }
}
catch (Exception e)
{
    using (ColoredConsole.Use(ConsoleColor.Red))
    {
        Console.WriteLine("Error starting setting up demo.");
        Console.WriteLine($"{e.Message}{Environment.NewLine}{e.StackTrace}");
    }
}

using (ColoredConsole.Use(ConsoleColor.Yellow))
{
    Console.WriteLine("Done, press ENTER.");
    Console.ReadLine();
}

void ScaleSalesEndpointIfRequired(DemoLauncher launcher, TaskCompletionSource<bool> syncEvent)
{
    _ = Task.Run(() =>
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("Press [up arrow] to scale out the Sales service or [down arrow] to scale in");
            Console.WriteLine("Press Ctrl+C stop Particular Monitoring Demo.");
            Console.WriteLine();

            while (!tokenSource.IsCancellationRequested)
            {
                var input = Console.ReadKey(true);
                switch (input.Key)
                {
                    case ConsoleKey.LeftArrow:
                        launcher.ScaleInSales();
                        break;
                    case ConsoleKey.RightArrow:
                        launcher.ScaleOutSales();
                        break;
                    default:
                        launcher.Send(new string(input.KeyChar, 1));
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            // surface any other exception
            syncEvent.TrySetException(e);
        }
    });
}
