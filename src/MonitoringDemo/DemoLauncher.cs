namespace MonitoringDemo
{
    using System;
    using System.Diagnostics;
    using System.IO;

    class DemoLauncher : IDisposable
    {
        readonly Job demoJob;
        private bool disposed;

        public DemoLauncher()
        {
            demoJob = new Job("Particular.MonitoringDemo");

            File.WriteAllText(@".\Marker.sln", string.Empty);
        }

        public void Platform()
        {
            if (disposed)
            {
                return;
            }

            var proc = StartProcess(@"Platform\net461\Platform.exe");
            demoJob.AddProcess(proc);
        }

        public void Billing()
        {
            if (disposed)
            {
                return;
            }

            var proc = StartProcess(@"Billing\net461\Billing.exe");
            demoJob.AddProcess(proc);
        }

        public void Shipping()
        {
            if (disposed)
            {
                return;
            }

            var proc = StartProcess(@"Shipping\net461\Shipping.exe");
            demoJob.AddProcess(proc);
        }

        public void Sales(string instanceId = null)
        {
            if (disposed)
            {
                return;
            }

            var proc = StartProcess(@"Sales\net461\Sales.exe", instanceId);
            demoJob.AddProcess(proc);
        }

        public void ClientUI()
        {
            if (disposed)
            {
                return;
            }

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
            disposed = true;

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