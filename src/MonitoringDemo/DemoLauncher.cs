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
            DirectoryEx.ForceDeleteReadonly(".db");
            DirectoryEx.ForceDeleteReadonly(".audit-db");
        }

        public void Platform()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Platform\net472\Platform.exe");
        }

        public void Billing()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Billing\net472\Billing.exe");
        }

        public void Shipping()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Shipping\net472\Shipping.exe");
        }

        public void ScaleOutSales()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"Sales\net472\Sales.exe");
        }

        public void ScaleInSales()
        {
            if (disposed)
            {
                return;
            }

            demoJob.KillProcess(@"Sales\net472\Sales.exe");
        }

        public void ClientUI()
        {
            if (disposed)
            {
                return;
            }

            demoJob.AddProcess(@"ClientUI\net472\ClientUI.exe");
        }

        readonly Job demoJob;
        private bool disposed;
    }
}