namespace MonitoringDemo
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    class Job : IDisposable
    {
        public Job(string jobName)
        {
            handle = CreateJobObject(IntPtr.Zero, jobName);

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
                processes = new List<Process>();
                processesByExec[relativeExePath] = processes;
            }

            var processesCount = processes.Count;
            var instanceId = processesCount == 0 ? null : processesCount.ToString();

            var process = StartProcess(relativeExePath, instanceId);

            processes.Add(process);

            return AddProcess(process);
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

        bool AddProcess(Process process) => AddProcess(process.Handle);

        bool AddProcess(IntPtr processHandle) => AssignProcessToJobObject(handle, processHandle);

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

            CloseHandle(handle);
            handle = IntPtr.Zero;
            processesByExec.Clear();
            disposed = true;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateJobObject(IntPtr a, string lpName);

        [DllImport("kernel32.dll")]
        static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        readonly Dictionary<string, List<Process>> processesByExec = new Dictionary<string, List<Process>>();

        IntPtr handle;
        bool disposed;
    }

    #region Helper classes

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
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
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
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
}