using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MonitoringDemo;

/// <summary>
/// Provides process group management functionality across Windows, Linux, and macOS.
/// Uses Windows Job Objects on Windows and Process Groups on Unix-like systems.
/// </summary>
partial class ProcessGroup : IDisposable
{
    readonly bool redirectInputAndOutput;
    readonly Dictionary<string, Stack<Process>> processesByAssemblyPath = [];
    readonly List<int> managedProcessIds = [];
    bool disposed;

    // Windows-specific fields
    nint jobHandle;

    public ProcessGroup(string groupName,bool redirectInputAndOutput)
    {
        this.redirectInputAndOutput = redirectInputAndOutput;
        if (OperatingSystem.IsWindows())
        {
            InitializeWindowsJob(groupName);
        }
    }

    public void Send(string relativeAssemblyPath, int index, string value)
    {
        if (!redirectInputAndOutput)
        {
            return;
        }

        if (processesByAssemblyPath.TryGetValue(relativeAssemblyPath, out var processes))
        {
            if (processes.Count > index)
            {
                processes.ElementAt(index).StandardInput.WriteLine(value);
            }
        }
    }

    public bool AddProcess(string relativeAssemblyPath)
    {
        if (!processesByAssemblyPath.TryGetValue(relativeAssemblyPath, out var processes))
        {
            processes = [];
            processesByAssemblyPath[relativeAssemblyPath] = processes;
        }

        var processesCount = processes.Count;
        var instanceId = processesCount == 0 ? null : $"instance-{processesCount}";

        var process = StartProcess(relativeAssemblyPath, instanceId);

        if (process is null)
        {
            return false;
        }

        if (redirectInputAndOutput)
        {
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            process.BeginOutputReadLine();
        }

        processes.Push(process);
        managedProcessIds.Add(process.Id);

        return OperatingSystem.IsWindows() ? AddProcessToWindowsJob(process)
            : (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && AddProcessToUnixGroup(process);
    }

    public void KillProcess(string relativeAssemblyPath)
    {
        if (!processesByAssemblyPath.TryGetValue(relativeAssemblyPath, out var processes))
        {
            return;
        }

        while (processes.TryPop(out var victim))
        {
            try
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    KillProcessGroupUnix(victim.Id);
                }
                else
                {
                    victim.Kill(true);
                }
                return;
            }
            catch (Exception)
            {
                // Process already terminated
            }
            finally
            {
                victim.Dispose();
            }
        }
    }

    #region Unix-specific implementations

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static bool AddProcessToUnixGroup(Process process)
    {
        try
        {
            // Set process group ID to child process ID (creating new group)
            return setpgid(process.Id, process.Id) == 0;
        }
        catch
        {
            return false;
        }
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private void KillProcessGroupUnix(int processId)
    {
        // Send SIGTERM to the entire process group
        kill(-processId, 15); // 15 is SIGTERM
    }

    // P/Invoke declarations for Unix systems
    [LibraryImport("libc", SetLastError = true)]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static partial int setpgid(int pid, int pgid);

    [LibraryImport("libc", SetLastError = true)]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static partial int kill(int pid, int sig);

    #endregion

    #region Windows-specific implementations

    [SupportedOSPlatform("windows")]
    private void InitializeWindowsJob(string jobName)
    {
        jobHandle = CreateJobObject(nint.Zero, jobName);

        var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            LimitFlags = 0x2000 // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        };

        var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = info
        };

        var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        var extendedInfoPtr = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

        try
        {
            if (!SetInformationJobObject(jobHandle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
            {
                throw new Exception($"Unable to set information. Error: {Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }
    }

    [SupportedOSPlatform("windows")]
    private bool AddProcessToWindowsJob(Process process) =>
        AssignProcessToJobObject(jobHandle, process.Handle);

    [SupportedOSPlatform("windows")]
    private void DisposeWindowsJob()
    {
        if (jobHandle != nint.Zero)
        {
            CloseHandle(jobHandle);
            jobHandle = nint.Zero;
        }
    }

    // P/Invoke declarations for Windows
    [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", StringMarshalling = StringMarshalling.Utf16)]
    [SupportedOSPlatform("windows")]
    private static partial nint CreateJobObject(nint a, string lpName);

    [LibraryImport("kernel32.dll")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetInformationJobObject(nint hJob, JobObjectInfoType infoType, nint lpJobObjectInfo, uint cbJobObjectInfoLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(nint job, nint process);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint hObject);

    #endregion

    private Process? StartProcess(string relativeAssemblyPath, string? arguments = null)
    {
        var fullAssemblyPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeAssemblyPath));
        var workingDirectory = Path.GetDirectoryName(fullAssemblyPath);

        var startInfo = new ProcessStartInfo("dotnet", fullAssemblyPath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = !redirectInputAndOutput,
            RedirectStandardInput = redirectInputAndOutput,
            RedirectStandardOutput = redirectInputAndOutput
        };

        if (arguments is not null)
        {
            startInfo.Arguments += $" {arguments} {!redirectInputAndOutput}";
        }

        return Process.Start(startInfo);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            if (OperatingSystem.IsWindows())
            {
                DisposeWindowsJob();
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Clean up any remaining processes on Unix systems
                foreach (var pid in managedProcessIds)
                {
                    try
                    {
                        KillProcessGroupUnix(pid);
                    }
                    catch
                    {
                        // Process might already be terminated
                    }
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Process management is not supported on this platform.");
            }

            processesByAssemblyPath.Clear();
            managedProcessIds.Clear();
        }

        disposed = true;
    }
}

#region Windows-specific structs and enums
#pragma warning disable PS0024
[StructLayout(LayoutKind.Sequential)]
file struct IO_COUNTERS
#pragma warning restore PS0024
{
    public ulong ReadOperationCount;
    public ulong WriteOperationCount;
    public ulong OtherOperationCount;
    public ulong ReadTransferCount;
    public ulong WriteTransferCount;
    public ulong OtherTransferCount;
}

[StructLayout(LayoutKind.Sequential)]
file struct JOBOBJECT_BASIC_LIMIT_INFORMATION
{
    public long PerProcessUserTimeLimit;
    public long PerJobUserTimeLimit;
    public uint LimitFlags;
    public nuint MinimumWorkingSetSize;
    public nuint MaximumWorkingSetSize;
    public uint ActiveProcessLimit;
    public nuint Affinity;
    public uint PriorityClass;
    public uint SchedulingClass;
}

[StructLayout(LayoutKind.Sequential)]
file struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
{
    public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
    public IO_COUNTERS IoInfo;
    public nuint ProcessMemoryLimit;
    public nuint JobMemoryLimit;
    public nuint PeakProcessMemoryUsed;
    public nuint PeakJobMemoryUsed;
}

internal enum JobObjectInfoType
{
    AssociateCompletionPortInformation = 7,
    BasicLimitInformation = 2,
    BasicUIRestrictions = 4,
    EndOfJobTimeInformation = 6,
    ExtendedLimitInformation = 9,
    SecurityLimitInformation = 5,
    GroupInformation = 11
}
#pragma warning restore PS0024
#endregion