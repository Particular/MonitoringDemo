namespace MonitoringDemo
{
    using System;
    using System.IO;

    class DemoLauncher : IDisposable
    {
        public DemoLauncher()
        {
            demoJob = new Job("Particular.MonitoringDemo");

            File.WriteAllText(@".\Marker.sln", string.Empty);
        }

        public void Dispose()
        {
            disposed = true;

            demoJob.Dispose();

            File.Delete(@".\Marker.sln");

            Console.WriteLine("Removing Transport Files");
            DirectoryEx.Delete(".learningtransport");

            Console.WriteLine("Deleting log folders");
            DirectoryEx.Delete(".logs");

            Console.WriteLine("Deleting db folders");
            DirectoryEx.Delete(".db");
        }

        public void Platform()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Platform\net461\Platform.exe");
        }

        public void Billing()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Billing\net461\Billing.exe");
        }

        public void Shipping()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Shipping\net461\Shipping.exe");
        }

        public void ScaleOutSales()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Sales\net461\Sales.exe");
        }

        public void ScaleInSales()
        {
            if (disposed)
            {
                return;
            }

            demoJob.KillProcess(@"Sales\net461\Sales.exe");
        }

        public void ClientUI()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"ClientUI\net461\ClientUI.exe");
        }

        readonly Job demoJob;
        private bool disposed;
    }
}