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
    readonly Dictionary<string, Stack<Process>> processesByExec = [];
    readonly List<int> managedProcessIds = [];
    bool disposed;

    // Windows-specific fields
    nint jobHandle;

    public ProcessGroup(string groupName)
    {
        if (OperatingSystem.IsWindows())
        {
            InitializeWindowsJob(groupName);
        }
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

        if (process is null)
        {
            return false;
        }

        processes.Push(process);
        managedProcessIds.Add(process.Id);

        return OperatingSystem.IsWindows() ? AddProcessToWindowsJob(process)
            : (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && AddProcessToUnixGroup(process);
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

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static partial int chmod(string path, int mode);

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

    private static Process? StartProcess(string relativeExePath, string? arguments = null)
    {
        // Handle platform-specific executable names
        var adjustedPath = relativeExePath;
        if (!OperatingSystem.IsWindows() && relativeExePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            // Remove .exe extension for non-Windows platforms
            adjustedPath = relativeExePath[..^4];
        }

        var fullExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, adjustedPath));
        var workingDirectory = Path.GetDirectoryName(fullExePath);

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // Ensure the file has execute permissions on Unix systems
            try
            {
                chmod(fullExePath, 0x755); // rwxr-xr-x permissions
            }
            catch
            {
                // If chmod fails, the Process.Start will likely fail too
            }
        }

        var startInfo = new ProcessStartInfo(fullExePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = OperatingSystem.IsWindows(),
            CreateNoWindow = !OperatingSystem.IsWindows(),
        };

        if (arguments is not null)
        {
            startInfo.Arguments = arguments;
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

            processesByExec.Clear();
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