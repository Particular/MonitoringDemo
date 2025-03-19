namespace MonitoringDemo;

sealed class DemoLauncher : IDisposable
{
    public DemoLauncher()
    {
        demoProcessGroup = new ProcessGroup("Particular.MonitoringDemo");

        File.WriteAllText(@".\Marker.sln", string.Empty);
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

        demoProcessGroup.AddProcess(Path.Combine("Billing", "Billing.dll"));
    }

    public void Shipping()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(Path.Combine("Shipping", "Shipping.dll"));
    }

    public void ScaleOutSales()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(Path.Combine("Sales", "Sales.dll"));
    }

    public void ScaleInSales()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.KillProcess(Path.Combine("Sales", "Sales.dll"));
    }

    public void ClientUI()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(Path.Combine("ClientUI", "ClientUI.dll"));
    }

    readonly ProcessGroup demoProcessGroup;
    private bool disposed;
}