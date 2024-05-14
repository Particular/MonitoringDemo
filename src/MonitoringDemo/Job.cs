namespace MonitoringDemo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class Job : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool AddProcess(string relativeExePath)
    {
        if (!processesByExec.TryGetValue(relativeExePath, out var processes))
        {
            processes = [];
            processesByExec[relativeExePath] = processes;
        }

        var processesCount = processes.Count;
        var instanceId = processesCount == 0 ? null : $"instance-{processesCount}";

        var process = StartProcess(relativeExePath, instanceId);

        processes.Add(process);

        return true;
    }

    public void KillProcess(string relativeExePath)
    {
        if (!processesByExec.TryGetValue(relativeExePath, out var processes))
        {
            return;
        }

        while (processes.Any())
        {
            var victim = processes.Last();
            try
            {
                victim.Kill();
                processes.Remove(victim);
                return;
            }
            catch (Exception)
            {
                //The process has died or has been killed by the user. Remove from the list and try to kill another one.
                processes.Remove(victim);
            }
        }
    }

    static Process StartProcess(string relativeExePath, string arguments = null)
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

    void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

        processesByExec.Clear();
        disposed = true;
    }

    readonly Dictionary<string, List<Process>> processesByExec = new();
        
    bool disposed;
}