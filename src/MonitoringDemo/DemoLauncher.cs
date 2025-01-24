namespace MonitoringDemo;

sealed class DemoLauncher : IDisposable
{
    readonly bool remoteControlMode;

    public DemoLauncher(bool remoteControlMode)
    {
        this.remoteControlMode = remoteControlMode;
        demoJob = new Job("Particular.MonitoringDemo", remoteControlMode);

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

    public void Send(string value)
    {
        demoJob.Send(billingPath, 0, value);
        demoJob.Send(shippingPath, 0, value);
        demoJob.Send(clientPath, 0, value);
        demoJob.Send(salesPath, 0, value);
    }

    public void Platform()
    {
        if (disposed)
        {
            return;
        }

        demoJob.AddProcess(Path.Combine("Platform", $"Platform.exe"));
    }

    public void Billing()
    {
        if (disposed)
        {
            return;
        }

        demoJob.AddProcess(billingPath);
    }

    public void Shipping()
    {
        if (disposed)
        {
            return;
        }

        demoJob.AddProcess(shippingPath);
    }

    public void ScaleOutSales()
    {
        if (disposed)
        {
            return;
        }

        demoJob.AddProcess(salesPath);
    }

    public void ScaleInSales()
    {
        if (disposed)
        {
            return;
        }

        demoJob.KillProcess(salesPath);
    }

    public void ClientUI()
    {
        if (disposed)
        {
            return;
        }

        demoJob.AddProcess(clientPath);
    }

    readonly Job demoJob;
    private bool disposed;
    private static readonly string billingPath = Path.Combine("Billing", "Billing.exe");
    private static readonly string shippingPath = Path.Combine("Shipping", "Shipping.exe");
    private static readonly string salesPath = Path.Combine("Sales", "Sales.exe");
    private static readonly string clientPath = Path.Combine("ClientUI", "ClientUI.exe");
}