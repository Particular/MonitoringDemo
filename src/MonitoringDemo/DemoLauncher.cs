namespace MonitoringDemo;

sealed class DemoLauncher : IDisposable
{
    public DemoLauncher(bool remoteControlMode)
    {
        demoProcessGroup = new ProcessGroup("Particular.MonitoringDemo", remoteControlMode);

        File.WriteAllText(@".\Marker.sln", string.Empty);
    }

    public void Send(string value)
    {
        demoProcessGroup.Send(BillingPath, 0, value);
        demoProcessGroup.Send(ShippingPath, 0, value);
        demoProcessGroup.Send(ClientPath, 0, value);
        demoProcessGroup.Send(SalesPath, 0, value);
    }

    public void Dispose()
    {
        disposed = true;

        demoProcessGroup.Dispose();

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

        demoProcessGroup.AddProcess(Path.Combine("PlatformLauncher", "PlatformLauncher.dll"));
    }

    public void Billing()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(BillingPath);
    }

    public void Shipping()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(ShippingPath);
    }

    public void ScaleOutSales()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(SalesPath);
    }

    public void ScaleInSales()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.KillProcess(SalesPath);
    }

    public void ClientUI()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(ClientPath);
    }

    readonly ProcessGroup demoProcessGroup;
    private bool disposed;

    private static readonly string BillingPath = Path.Combine("Billing", "Billing.dll");
    private static readonly string ShippingPath = Path.Combine("Shipping", "Shipping.dll");
    private static readonly string SalesPath = Path.Combine("Sales", "Sales.dll");
    private static readonly string ClientPath = Path.Combine("ClientUI", "ClientUI.dll");
}