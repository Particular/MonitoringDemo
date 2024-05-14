namespace MonitoringDemo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

sealed class ProcessGroup : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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

        processes.Push(process);
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

    readonly Dictionary<string, Stack<Process>> processesByExec = new();
        
    bool disposed;
}