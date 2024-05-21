using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MonitoringDemo;

partial class Job : IDisposable
{
    public Job(string jobName)
    {
        handle = CreateJobObject(nint.Zero, jobName);

        var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            LimitFlags = 0x2000
        };

        var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = info
        };

        var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        var extendedInfoPtr = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

        if (!SetInformationJobObject(handle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
        {
            throw new Exception($"Unable to set information. Error: {Marshal.GetLastWin32Error()}");
        }
    }

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

        if (process is null)
        {
            return false;
        }

        processes.Push(process);

        return AddProcess(process);
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

    bool AddProcess(Process process) => AddProcess(process.Handle);

    bool AddProcess(nint processHandle) => AssignProcessToJobObject(handle, processHandle);

    static Process? StartProcess(string relativeExePath, string? arguments = null)
    {
        var fullExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeExePath));
        var workingDirectory = Path.GetDirectoryName(fullExePath);

        var startInfo = new ProcessStartInfo(fullExePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };

        if (arguments is not null)
        {
            startInfo.Arguments = arguments;
        }

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

        CloseHandle(handle);
        handle = nint.Zero;
        processesByExec.Clear();
        disposed = true;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateJobObject(nint a, string lpName);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetInformationJobObject(nint hJob, JobObjectInfoType infoType, nint lpJobObjectInfo, uint cbJobObjectInfoLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(nint job, nint process);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint hObject);

    readonly Dictionary<string, Stack<Process>> processesByExec = [];

    nint handle;
    bool disposed;
}

#region Helper classes

[StructLayout(LayoutKind.Sequential)]
#pragma warning disable PS0024 // A non-interface type should not be prefixed with I
struct IO_COUNTERS
#pragma warning restore PS0024 // A non-interface type should not be prefixed with I
{
    public ulong ReadOperationCount;
    public ulong WriteOperationCount;
    public ulong OtherOperationCount;
    public ulong ReadTransferCount;
    public ulong WriteTransferCount;
    public ulong OtherTransferCount;
}


[StructLayout(LayoutKind.Sequential)]
struct JOBOBJECT_BASIC_LIMIT_INFORMATION
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
struct SECURITY_ATTRIBUTES
{
    public uint nLength;
    public nint lpSecurityDescriptor;
    public int bInheritHandle;
}

[StructLayout(LayoutKind.Sequential)]
struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
{
    public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
    public IO_COUNTERS IoInfo;
    public nuint ProcessMemoryLimit;
    public nuint JobMemoryLimit;
    public nuint PeakProcessMemoryUsed;
    public nuint PeakJobMemoryUsed;
}

enum JobObjectInfoType
{
    AssociateCompletionPortInformation = 7,
    BasicLimitInformation = 2,
    BasicUIRestrictions = 4,
    EndOfJobTimeInformation = 6,
    ExtendedLimitInformation = 9,
    SecurityLimitInformation = 5,
    GroupInformation = 11
}

#endregion
