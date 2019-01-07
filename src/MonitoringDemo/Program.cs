namespace MonitoringDemo
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    class Program
    {
        static readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        static void Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("The Particular Monitoring Demo can currently only be run on the Windows platform.");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            var wait = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                wait.Set();
                tokenSource.Cancel();
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
                        launcher.Sales();

                        Console.WriteLine("Starting Shipping endpoint.");
                        launcher.Shipping();

                        Console.WriteLine("Starting ClientUI endpoint.");
                        launcher.ClientUI();

                        using (ColoredConsole.Use(ConsoleColor.Yellow))
                        {
                            // TODO Implement scaling

                            /*
                             *
Write-Host "Scaling out Sales endpoint"
Start-Process ".\Sales\net461\Sales.exe"  -ArgumentList "instance-1" -WorkingDirectory ".\Sales\net461\"
Start-Sleep -Seconds 20
Start-Process ".\Sales\net461\Sales.exe"  -ArgumentList "instance-2" -WorkingDirectory ".\Sales\net461\"
Start-Sleep -Seconds 20
Start-Process ".\Sales\net461\Sales.exe"  -ArgumentList "instance-3" -WorkingDirectory ".\Sales\net461\"
                             * 
                             */

                            Console.WriteLine("Press Ctrl+C stop Particular Monitoring Demo.");
                            wait.WaitOne();
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
    }
}
