namespace MonitoringDemo;

sealed class DemoLauncher : IDisposable
{
    public DemoLauncher()
    {
        demoProcessGroup = new ProcessGroup("Particular.MonitoringDemo");
    }

    public void Dispose()
    {
        disposed = true;

        demoProcessGroup.Dispose();

        //Console.WriteLine("Removing Transport Files");
        DirectoryEx.Delete(".learningtransport");

        //Console.WriteLine("Deleting log folders");
        DirectoryEx.Delete(".logs");

        //Console.WriteLine("Deleting db folders");
        DirectoryEx.ForceDeleteReadonly(".db");
        DirectoryEx.ForceDeleteReadonly(".audit-db");
    }

    public ProcessHandle AddProcess(string name, string instanceId)
    {
        if (disposed)
        {
            return ProcessHandle.Empty;
        }

        var path = Path.Combine(name, $"{name}.dll"); //TODO: Hard-coded convention
        return demoProcessGroup.AddProcess(path, instanceId);
    }

    readonly ProcessGroup demoProcessGroup;
    private bool disposed;

    private static readonly string BillingPath = Path.Combine("Billing", "Billing.dll");
    private static readonly string ShippingPath = Path.Combine("Shipping", "Shipping.dll");
    private static readonly string SalesPath = Path.Combine("Sales", "Sales.dll");
    private static readonly string ClientPath = Path.Combine("ClientUI", "ClientUI.dll");
    private static readonly string PlatformPath = Path.Combine("PlatformLauncher", "PlatformLauncher.dll");

    
}