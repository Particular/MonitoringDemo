using System.Diagnostics;

namespace MonitoringDemo;

sealed class ProcessGroup : IDisposable
{
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        // Strictly speaking this isn't required because all processes are spawned as child processes of the current
        // process and therefore will close when the current process closes. However, it's good practice to dispose
        // of all resources that implement IDisposable.
        foreach (var (_, processes) in processesByExec)
        {
            while (processes.TryPop(out var process))
            {
                process.Dispose();
            }
        }

        processesByExec.Clear();
        disposed = true;
    }

    public void AddProcess(string relativeExePath)
    {
        if (!processesByExec.TryGetValue(relativeExePath, out var processes))
        {
            processes = [];
            processesByExec[relativeExePath] = processes;
        }

        var processesCount = processes.Count;
        var instanceId = processesCount == 0 ? null : $"instance-{processesCount}";

        var process = StartProcess(relativeExePath, instanceId);

        if (process is not null)
        {
            processes.Push(process);
        }
    }

    public void KillProcess(string relativeExePath)
    {
        if (!processesByExec.TryGetValue(relativeExePath, out var processes))
        {
            return;
        }

        while (processes.TryPop(out var victim))
        {
            try
            {
                victim.Kill(true);
                return;
            }
            catch (Exception)
            {
                //The process has died or has been killed by the user. Let's try to kill another one by doing at
                // least another iteration
            }
            finally
            {
                victim.Dispose();
            }
        }
    }

    static Process? StartProcess(string relativeExePath, string? arguments = null)
    {
        var fullExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeExePath));
        var workingDirectory = Path.GetDirectoryName(fullExePath);

        var startInfo = new ProcessStartInfo(fullExePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false
        };

        if (arguments is not null)
        {
            startInfo.Arguments = arguments;
        }

        return Process.Start(startInfo);
    }

    readonly Dictionary<string, Stack<Process>> processesByExec = [];

    bool disposed;
}