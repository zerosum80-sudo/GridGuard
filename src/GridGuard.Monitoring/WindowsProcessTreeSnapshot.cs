using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GridGuard.Monitoring;

[SupportedOSPlatform("windows")]
internal static class WindowsProcessTreeSnapshot
{
    private const uint SnapshotProcesses = 0x00000002;
    private static readonly nint InvalidHandle = new(-1);

    public static Dictionary<int, int> Capture(List<string> errors)
    {
        var result = new Dictionary<int, int>();
        var handle = CreateToolhelp32Snapshot(SnapshotProcesses, 0);
        if (handle == InvalidHandle)
        {
            errors.Add("processTree:Win32SnapshotFailed");
            return result;
        }
        try
        {
            var entry = new ProcessEntry32
            {
                Size = (uint)Marshal.SizeOf<ProcessEntry32>()
            };
            if (!Process32First(handle, ref entry)) return result;
            do
            {
                result[(int)entry.ProcessId] = (int)entry.ParentProcessId;
                entry.Size = (uint)Marshal.SizeOf<ProcessEntry32>();
            }
            while (Process32Next(handle, ref entry));
        }
        finally
        {
            CloseHandle(handle);
        }
        return result;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public nint DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int BasePriority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExecutableFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "Process32FirstW",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(nint snapshot, ref ProcessEntry32 entry);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "Process32NextW",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(nint snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
}
