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

    public ProcessHandle AddProcess(string name, string instanceId, int port)
    {
        if (disposed)
        {
            return ProcessHandle.Empty;
        }

        var path = Path.Combine("..", name, $"{name}.dll"); //TODO: Hard-coded convention
        return demoProcessGroup.AddProcess(path, instanceId, port);
    }

    readonly ProcessGroup demoProcessGroup;
    private bool disposed;
}