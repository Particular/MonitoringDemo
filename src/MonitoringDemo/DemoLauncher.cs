namespace MonitoringDemo
{
    using System;
    using System.Diagnostics;
    using System.IO;

    class DemoLauncher : IDisposable
    {
        readonly Job demoJob;

        public DemoLauncher()
        {
            demoJob = new Job("Particular.MonitoringDemo");

            File.WriteAllText(@".\Marker.sln", string.Empty);
        }

        public void Platform()
        {
            var proc = StartProcess(@"Platform\net461\Platform.exe");
            demoJob.AddProcess(proc);
        }

        public void Billing()
        {
            var proc = StartProcess(@"Billing\net461\Billing.exe");
            demoJob.AddProcess(proc);
        }

        public void Shipping()
        {
            var proc = StartProcess(@"Shipping\net461\Shipping.exe");
            demoJob.AddProcess(proc);
        }

        public void Sales()
        {
            var proc = StartProcess(@"Sales\net461\Sales.exe");
            demoJob.AddProcess(proc);
        }

        public void ClientUI()
        {
            var proc = StartProcess(@"ClientUI\net461\ClientUI.exe");
            demoJob.AddProcess(proc);
        }

        Process StartProcess(string relativeExePath, string arguments = null)
        {
            var fullExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeExePath));
            var workingDirectory = Path.GetDirectoryName(fullExePath);

            var startInfo = new ProcessStartInfo(fullExePath, arguments)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            };

            return Process.Start(startInfo);
        }

        public void Dispose()
        {
            demoJob.Dispose();

            File.Delete(@".\Marker.sln");

            Console.WriteLine("Removing Transport Files");
            DirectoryEx.Delete(@".learningtransport");

            Console.WriteLine("Deleting log folders");
            DirectoryEx.Delete(@".logs");

            Console.WriteLine("Deleting db folders");
            DirectoryEx.Delete(@".db");
        }
    }
}