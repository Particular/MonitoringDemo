namespace MonitoringDemo;

sealed class DemoLauncher : IDisposable
{
    public DemoLauncher()
    {
        demoProcessGroup = new ProcessGroup();

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

        demoProcessGroup.AddProcess(Path.Combine("Platform", "net8.0", $"Platform{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}"));
    }

    public void Billing()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(Path.Combine("Billing", "net8.0", $"Billing{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}"));
    }

    public void Shipping()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(Path.Combine("Shipping", "net8.0", $"Shipping{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}"));
    }

    public void ScaleOutSales()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(Path.Combine("Sales", "net8.0", $"Sales{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}"));
    }

    public void ScaleInSales()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.KillProcess(Path.Combine("Sales", "net8.0", $"Sales{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}"));
    }

    public void ClientUI()
    {
        if (disposed)
        {
            return;
        }

        demoProcessGroup.AddProcess(Path.Combine("ClientUI", "net8.0", $"ClientUI{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}"));
    }

    readonly ProcessGroup demoProcessGroup;
    private bool disposed;
}