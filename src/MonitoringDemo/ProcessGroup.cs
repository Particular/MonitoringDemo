﻿namespace MonitoringDemo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

sealed class ProcessGroup : IDisposable
{
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

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
            UseShellExecute = false
        };

        return Process.Start(startInfo);
    }

    readonly Dictionary<string, Stack<Process>> processesByExec = new();
        
    bool disposed;
}