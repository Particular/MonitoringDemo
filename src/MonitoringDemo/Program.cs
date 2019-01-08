﻿namespace MonitoringDemo
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            var syncEvent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                tokenSource.Cancel();
                syncEvent.TrySetResult(true);
            };

            try
            {
                using (var launcher = new DemoLauncher())
                {
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

                            await syncEvent.Task.ConfigureAwait(false);

                            Console.WriteLine("Shutting down");
                        }
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
        }

        private static void ScaleSalesEndpointIfRequired(DemoLauncher launcher, TaskCompletionSource<bool> syncEvent)
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("Press O to scale out the Sales service or I to scale in");
                    Console.WriteLine("Press Ctrl+C stop Particular Monitoring Demo.");
                    Console.WriteLine();

                    while (!tokenSource.IsCancellationRequested)
                    {
                        var input = Console.ReadKey(true);

                        if (input.Key == ConsoleKey.I)
                        {
                            launcher.ScaleInSales();
                        }
                        if (input.Key == ConsoleKey.O)
                        {
                            launcher.ScaleOutSales();
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
    }
}
