using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using GridGuard.Core;
using Microsoft.Win32;

namespace GridGuard.Response;

public sealed record GridAutoRemovalOptions
{
    public bool Enabled { get; init; }
    public string AuthorizedRuleId { get; init; } = GridAutoRemovalPolicy.RuleId;
    public string ServiceName { get; init; } = GridAutoRemovalPolicy.ServiceName;
    public string AllowedComponentPath { get; init; } =
        @"%ProgramFiles(x86)%\NAT Service\natsvc.exe";
    public string LogPath { get; init; } =
        @"%ProgramData%\GridGuard\logs\auto-removal.jsonl";
    public string RulePath { get; init; } =
        @"rules\candidate\grid.natservice.001.json";
    public int MonitorSeconds { get; init; } = 2;
}

public static class GridAutoRemovalPolicy
{
    public const string RuleId = "grid.natservice.001";
    public const string ServiceName = "NATService";
    public const string ComponentFileName = "natsvc.exe";
    public const string ComponentPathSuffix = @"\NAT Service\natsvc.exe";
    public static string ExpectedComponentPath => Path.GetFullPath(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        "NAT Service",
        ComponentFileName));

    public static IReadOnlyList<string> Validate(GridAutoRemovalOptions options)
    {
        var errors = new List<string>();
        if (!string.Equals(options.AuthorizedRuleId, RuleId, StringComparison.Ordinal))
            errors.Add($"AuthorizedRuleId must be exactly {RuleId}.");
        if (!string.Equals(options.ServiceName, ServiceName, StringComparison.Ordinal))
            errors.Add($"ServiceName must be exactly {ServiceName}.");
        var path = ExpandedPath(options.AllowedComponentPath);
        if (!path.Equals(ExpectedComponentPath, StringComparison.OrdinalIgnoreCase))
            errors.Add($"AllowedComponentPath must be exactly {ExpectedComponentPath}.");
        if (options.MonitorSeconds is < 1 or > 300)
            errors.Add("MonitorSeconds must be between 1 and 300.");
        return errors;
    }

    public static bool Authorizes(
        GridAutoRemovalOptions options,
        DetectionResult detection,
        out string reason)
    {
        var errors = Validate(options);
        if (errors.Count > 0)
        {
            reason = string.Join(" ", errors);
            return false;
        }
        if (!options.Enabled)
        {
            reason = "Automatic removal is disabled.";
            return false;
        }
        if (!string.Equals(detection.RuleId, RuleId, StringComparison.Ordinal))
        {
            reason = "Detection rule is outside the exact automatic-removal allowlist.";
            return false;
        }
        if (detection.Decision is not (DetectionDecision.Suspicious or
            DetectionDecision.Confirmed))
        {
            reason = "Detection did not match.";
            return false;
        }

        var matchingServiceObjects = detection.Evidence
            .Where(item => item.Type.Equals("serviceName", StringComparison.OrdinalIgnoreCase))
            .Where(item => item.Value.Equals(ServiceName, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.ObjectId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matchingPathObjects = detection.Evidence
            .Where(item =>
                item.Type.Equals("serviceImagePath", StringComparison.OrdinalIgnoreCase))
            .Where(item => NormalizeExecutablePath(item.Value).Equals(
                ExpandedPath(options.AllowedComponentPath),
                StringComparison.OrdinalIgnoreCase))
            .Select(item => item.ObjectId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!matchingServiceObjects.Overlaps(matchingPathObjects))
        {
            reason = "Exact NATService name and exact allowed natsvc.exe path " +
                "must belong to the same service object.";
            return false;
        }
        reason = "Exact automatic-removal policy matched.";
        return true;
    }

    public static string ExpandedPath(string value) =>
        Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim().Trim('"')));

    public static string NormalizeExecutablePath(string value)
    {
        var expanded = Environment.ExpandEnvironmentVariables(value.Trim());
        if (expanded.StartsWith('"'))
        {
            var closing = expanded.IndexOf('"', 1);
            if (closing > 1) expanded = expanded[1..closing];
        }
        else if (expanded.Contains(".exe ", StringComparison.OrdinalIgnoreCase))
        {
            expanded = expanded[..(expanded.IndexOf(".exe ",
                StringComparison.OrdinalIgnoreCase) + 4)];
        }
        return Path.GetFullPath(expanded.Trim('"'));
    }
}

public sealed record GridComponentPresence(
    bool ServicePresent,
    bool ProcessPresent,
    bool FilePresent,
    string? ServiceImagePath);

public interface IGridComponentHost
{
    Task<GridComponentPresence> InspectAsync(CancellationToken cancellationToken);
    Task StopComponentAsync(string serviceName, CancellationToken cancellationToken);
    Task DeleteComponentFileAsync(string path, CancellationToken cancellationToken);
    Task DeleteServiceAsync(string serviceName, CancellationToken cancellationToken);
}

public interface IGridRemovalVerifier
{
    Task<bool> IsNoMatchAsync(CancellationToken cancellationToken);
}

public sealed record GridRemovalAuditRecord(
    DateTimeOffset DetectionTime,
    string RuleId,
    string Status,
    string? RemovedService,
    IReadOnlyList<string> RemovedFiles,
    string VerificationResult,
    IReadOnlyList<string> Errors);

public interface IGridRemovalAuditSink
{
    Task WriteAsync(
        GridRemovalAuditRecord record,
        CancellationToken cancellationToken = default);
}

public sealed class JsonLineGridRemovalAuditSink(string path) : IGridRemovalAuditSink
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = GridAutoRemovalPolicy.ExpandedPath(path);

    public async Task WriteAsync(
        GridRemovalAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.AppendAllTextAsync(
                _path,
                JsonSerializer.Serialize(record) + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class GridAutoRemovalWorkflow(
    GridAutoRemovalOptions options,
    IGridComponentHost host,
    IGridRemovalVerifier verifier,
    IGridRemovalAuditSink audit)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<GridRemovalAuditRecord> ExecuteAsync(
        DetectionResult detection,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!GridAutoRemovalPolicy.Authorizes(options, detection, out var reason))
                return await RecordAsync(new(
                    detection.Timestamp,
                    detection.RuleId,
                    "REFUSED",
                    null,
                    [],
                    "NOT_RUN",
                    [reason]), cancellationToken);

            var allowedPath = GridAutoRemovalPolicy.ExpandedPath(
                options.AllowedComponentPath);
            var removedFiles = new List<string>();
            var errors = new List<string>();
            string? removedService = null;
            var before = await host.InspectAsync(cancellationToken);

            try
            {
                await host.StopComponentAsync(
                    GridAutoRemovalPolicy.ServiceName, cancellationToken);
                await host.DeleteComponentFileAsync(allowedPath, cancellationToken);
                if (before.FilePresent) removedFiles.Add(allowedPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                InvalidOperationException)
            {
                errors.Add($"component:{ex.GetType().Name}:{ex.Message}");
            }

            if (errors.Count == 0)
            {
                try
                {
                    await host.DeleteServiceAsync(
                        GridAutoRemovalPolicy.ServiceName, cancellationToken);
                    if (before.ServicePresent)
                        removedService = GridAutoRemovalPolicy.ServiceName;
                }
                catch (Exception ex) when (ex is IOException or
                    UnauthorizedAccessException or InvalidOperationException)
                {
                    errors.Add($"service:{ex.GetType().Name}:{ex.Message}");
                }
            }

            var presence = await host.InspectAsync(cancellationToken);
            var noMatch = await verifier.IsNoMatchAsync(cancellationToken);
            var verified = !presence.ServicePresent && !presence.ProcessPresent &&
                !presence.FilePresent && noMatch;
            if (!verified)
                errors.Add("verification:NATService, natsvc.exe, or rule match remains.");

            return await RecordAsync(new(
                detection.Timestamp,
                detection.RuleId,
                errors.Count == 0 && verified ? "REMOVED" : "FAILED",
                removedService,
                removedFiles,
                verified ? "NATSERVICE_ABSENT_PROCESS_ABSENT_FILE_ABSENT_RULE_NO_MATCH" :
                    "VERIFICATION_FAILED",
                errors), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<GridRemovalAuditRecord> RecordAsync(
        GridRemovalAuditRecord record,
        CancellationToken cancellationToken)
    {
        await audit.WriteAsync(record, cancellationToken);
        return record;
    }
}

[SupportedOSPlatform("windows")]
public sealed class WindowsGridComponentHost(GridAutoRemovalOptions options)
    : IGridComponentHost
{
    private readonly string _allowedPath =
        GridAutoRemovalPolicy.ExpandedPath(options.AllowedComponentPath);

    public Task<GridComponentPresence> InspectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();
        using var service = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\{GridAutoRemovalPolicy.ServiceName}");
        var servicePath = service?.GetValue("ImagePath")?.ToString();
        var processes = Process.GetProcessesByName(
            Path.GetFileNameWithoutExtension(GridAutoRemovalPolicy.ComponentFileName));
        try
        {
            return Task.FromResult(new GridComponentPresence(
                service is not null,
                processes.Length > 0,
                File.Exists(_allowedPath),
                servicePath));
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }

    public async Task StopComponentAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        EnsureExactService(serviceName);
        var presence = await InspectAsync(cancellationToken);
        if (!presence.ServicePresent || !presence.ProcessPresent) return;
        await RunScAsync(["stop", GridAutoRemovalPolicy.ServiceName], cancellationToken);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (!(await InspectAsync(cancellationToken)).ProcessPresent) return;
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
        throw new InvalidOperationException("NATService process did not stop.");
    }

    public Task DeleteComponentFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requested = Path.GetFullPath(path);
        if (!requested.Equals(_allowedPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Component path is outside the exact allowlist.");
        if (File.Exists(requested)) File.Delete(requested);
        return Task.CompletedTask;
    }

    public async Task DeleteServiceAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        EnsureExactService(serviceName);
        if (!(await InspectAsync(cancellationToken)).ServicePresent) return;
        await RunScAsync(["delete", GridAutoRemovalPolicy.ServiceName], cancellationToken);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (!(await InspectAsync(cancellationToken)).ServicePresent) return;
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
        throw new InvalidOperationException(
            "NATService registration did not become absent.");
    }

    private static void EnsureExactService(string serviceName)
    {
        EnsureWindows();
        if (!serviceName.Equals(
                GridAutoRemovalPolicy.ServiceName, StringComparison.Ordinal))
            throw new InvalidOperationException("Service is outside the exact allowlist.");
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows is required.");
    }

    private static async Task RunScAsync(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo
        {
            FileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Unable to start Service Control.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var standardOutput = await stdout;
        var standardError = await stderr;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Service Control failed ({process.ExitCode}): " +
                $"{standardOutput} {standardError}".Trim());
    }
}
