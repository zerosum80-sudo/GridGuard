using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace GridGuard.Monitoring;

public sealed record InventoryRecord(
    string Kind,
    string Id,
    IReadOnlyDictionary<string, string> Properties);

public sealed record InventorySnapshot(
    DateTimeOffset CapturedAt,
    IReadOnlyList<InventoryRecord> Records,
    IReadOnlyList<string> Errors);

public interface IInventoryAdapter
{
    Task<InventorySnapshot> CaptureAsync(CancellationToken cancellationToken = default);
}

public sealed class WindowsInventoryAdapter(IEnumerable<string>? selectedDirectories = null)
    : IInventoryAdapter
{
    private readonly string[] _selectedDirectories = selectedDirectories?.ToArray() ?? [];

    public Task<InventorySnapshot> CaptureAsync(CancellationToken cancellationToken = default)
    {
        var records = new List<InventoryRecord>();
        var errors = new List<string>();
        CollectProcesses(records, errors);
        if (OperatingSystem.IsWindows())
        {
            CollectServices(records, errors);
            CollectAutoruns(records, errors);
            CollectScheduledTasks(records, errors);
            CollectStartup(records, errors);
        }
        CollectFiles(records, errors, cancellationToken);
        return Task.FromResult(new InventorySnapshot(DateTimeOffset.UtcNow, records, errors));
    }

    private static void CollectProcesses(List<InventoryRecord> records, List<string> errors)
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                string processName;
                try
                {
                    processName = process.ProcessName;
                }
                catch (Exception ex) when (ex is InvalidOperationException or
                    System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    errors.Add($"process:{process.Id}:{ex.GetType().Name}");
                    continue;
                }
                var executablePath = "";
                try
                {
                    executablePath = process.MainModule?.FileName ?? "";
                }
                catch (Exception ex) when (ex is InvalidOperationException or
                    System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    errors.Add($"processPath:{process.Id}:{ex.GetType().Name}");
                }
                records.Add(new("process", process.Id.ToString(), new Dictionary<string, string>
                {
                    ["processName"] = processName,
                    ["executablePath"] = executablePath
                }));
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void CollectServices(List<InventoryRecord> records, List<string> errors)
    {
        try
        {
            using var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            foreach (var name in services?.GetSubKeyNames() ?? [])
            {
                using var service = services!.OpenSubKey(name);
                records.Add(new("service", name, new Dictionary<string, string>
                {
                    ["serviceName"] = name,
                    ["serviceDisplayName"] = service?.GetValue("DisplayName")?.ToString() ?? "",
                    ["serviceImagePath"] = service?.GetValue("ImagePath")?.ToString() ?? "",
                    ["serviceStartType"] = service?.GetValue("Start")?.ToString() ?? "",
                    ["serviceState"] = "unresolved-read-only-registry-adapter"
                }));
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            errors.Add($"services:{ex.GetType().Name}");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void CollectAutoruns(List<InventoryRecord> records, List<string> errors)
    {
        var locations = new[]
        {
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
            (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce")
        };
        foreach (var (root, path) in locations)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                foreach (var name in key?.GetValueNames() ?? [])
                    records.Add(new("autorun", $"{root.Name}\\{path}\\{name}",
                        new Dictionary<string, string>
                        {
                            ["registryPath"] = $"{root.Name}\\{path}",
                            ["entryName"] = name,
                            ["commandLine"] = key!.GetValue(name)?.ToString() ?? ""
                        }));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                errors.Add($"autorun:{path}:{ex.GetType().Name}");
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void CollectScheduledTasks(List<InventoryRecord> records, List<string> errors)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32", "Tasks");
        CollectDirectory(root, "scheduledTask", records, errors, CancellationToken.None);
    }

    [SupportedOSPlatform("windows")]
    private static void CollectStartup(List<InventoryRecord> records, List<string> errors)
    {
        foreach (var folder in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
        }.Where(path => !string.IsNullOrWhiteSpace(path)))
            CollectDirectory(folder, "startupEntry", records, errors, CancellationToken.None);
    }

    private void CollectFiles(
        List<InventoryRecord> records,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var directory in _selectedDirectories)
            CollectDirectory(directory, "file", records, errors, cancellationToken);
    }

    private static void CollectDirectory(
        string root,
        string kind,
        List<InventoryRecord> records,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(root)) return;
            foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var info = new FileInfo(path);
                records.Add(new(kind, path, new Dictionary<string, string>
                {
                    ["path"] = path,
                    ["size"] = info.Length.ToString(),
                    ["lastWriteUtc"] = info.LastWriteTimeUtc.ToString("O")
                }));
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            errors.Add($"{kind}:{root}:{ex.GetType().Name}");
        }
    }
}
