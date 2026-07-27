using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace GridGuard.Monitoring;

public enum HypervisorKind
{
    HyperV,
    VMware,
    VirtualBox
}

public sealed record HypervisorCommandPlan(
    HypervisorKind Kind,
    string Operation,
    string Executable,
    IReadOnlyList<string> Arguments,
    bool RequiresHumanApproval);

public interface IVmHypervisorAdapter
{
    HypervisorKind Kind { get; }
    bool IsAvailable { get; }
    HypervisorCommandPlan PlanSnapshot(string vmName, string snapshotName);
    HypervisorCommandPlan PlanRollback(string vmName, string snapshotName);
}

public abstract class PlanningOnlyHypervisorAdapter(
    HypervisorKind kind,
    bool isAvailable) : IVmHypervisorAdapter
{
    public HypervisorKind Kind { get; } = kind;
    public bool IsAvailable { get; } = isAvailable;

    public abstract HypervisorCommandPlan PlanSnapshot(string vmName, string snapshotName);
    public abstract HypervisorCommandPlan PlanRollback(string vmName, string snapshotName);

    protected static void ValidateNames(string vmName, string snapshotName)
    {
        if (string.IsNullOrWhiteSpace(vmName) || string.IsNullOrWhiteSpace(snapshotName))
            throw new ArgumentException("VM and snapshot names are required.");
    }
}

public sealed class HyperVPlanningAdapter(bool isAvailable = false)
    : PlanningOnlyHypervisorAdapter(HypervisorKind.HyperV, isAvailable)
{
    public override HypervisorCommandPlan PlanSnapshot(string vmName, string snapshotName)
    {
        ValidateNames(vmName, snapshotName);
        return new(Kind, "snapshot", "Checkpoint-VM",
            ["-Name", vmName, "-SnapshotName", snapshotName], true);
    }

    public override HypervisorCommandPlan PlanRollback(string vmName, string snapshotName)
    {
        ValidateNames(vmName, snapshotName);
        return new(Kind, "rollback", "Restore-VMSnapshot",
            ["-VMName", vmName, "-Name", snapshotName, "-Confirm"], true);
    }
}

public sealed class VMwarePlanningAdapter(bool isAvailable = false)
    : PlanningOnlyHypervisorAdapter(HypervisorKind.VMware, isAvailable)
{
    public override HypervisorCommandPlan PlanSnapshot(string vmName, string snapshotName)
    {
        ValidateNames(vmName, snapshotName);
        return new(Kind, "snapshot", "vmrun",
            ["snapshot", vmName, snapshotName], true);
    }

    public override HypervisorCommandPlan PlanRollback(string vmName, string snapshotName)
    {
        ValidateNames(vmName, snapshotName);
        return new(Kind, "rollback", "vmrun",
            ["revertToSnapshot", vmName, snapshotName], true);
    }
}

public sealed class VirtualBoxPlanningAdapter(bool isAvailable = false)
    : PlanningOnlyHypervisorAdapter(HypervisorKind.VirtualBox, isAvailable)
{
    public override HypervisorCommandPlan PlanSnapshot(string vmName, string snapshotName)
    {
        ValidateNames(vmName, snapshotName);
        return new(Kind, "snapshot", "VBoxManage",
            ["snapshot", vmName, "take", snapshotName], true);
    }

    public override HypervisorCommandPlan PlanRollback(string vmName, string snapshotName)
    {
        ValidateNames(vmName, snapshotName);
        return new(Kind, "rollback", "VBoxManage",
            ["snapshot", vmName, "restore", snapshotName], true);
    }
}

public sealed record VmPreparationStatus(
    string State,
    IReadOnlyList<HypervisorKind> Supported,
    IReadOnlyList<HypervisorKind> Available);

public static class HypervisorAbstraction
{
    public static IReadOnlyList<IVmHypervisorAdapter> CreateDetectedAdapters() =>
    [
        new HyperVPlanningAdapter(File.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"WindowsPowerShell\v1.0\Modules\Hyper-V\Hyper-V.psd1"))),
        new VMwarePlanningAdapter(CommandExists(
            "vmrun.exe",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"VMware\VMware Workstation\vmrun.exe"))),
        new VirtualBoxPlanningAdapter(CommandExists(
            "VBoxManage.exe",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Oracle\VirtualBox\VBoxManage.exe")))
    ];

    public static VmPreparationStatus Inspect(IEnumerable<IVmHypervisorAdapter> adapters)
    {
        var items = adapters.ToArray();
        var supported = items.Select(item => item.Kind).Distinct().ToArray();
        var available = items.Where(item => item.IsAvailable)
            .Select(item => item.Kind).Distinct().ToArray();
        return new(
            available.Length == 0 ? "READY_FOR_VM" : "HYPERVISOR_AVAILABLE",
            supported,
            available);
    }

    private static bool CommandExists(string executable, string knownPath)
    {
        if (File.Exists(knownPath)) return true;
        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory =>
            {
                try
                {
                    return File.Exists(Path.Combine(directory.Trim('"'), executable));
                }
                catch (ArgumentException)
                {
                    return false;
                }
            });
    }
}

public sealed record VmWorkflowStep(
    int Order,
    string Name,
    string State,
    bool RequiresHumanApproval,
    bool MayMutateGuest);

public sealed record VmWorkflowPlan(
    string ContractId,
    string SafetyMode,
    bool RuntimeActive,
    IReadOnlyList<VmWorkflowStep> Steps);

public static class VmWorkflowPlanner
{
    public static VmWorkflowPlan Create() => new(
        "GRIDGUARD-VM-BEHAVIORAL-PREPARATION-V1",
        "AuditOnly/Simulate",
        false,
        [
            new(1, "Clean Snapshot", "PREPARED", false, false),
            new(2, "Install target software", "BLOCKED_BY_HUMAN_APPROVAL", true, true),
            new(3, "Automatic snapshot", "PREPARED", true, false),
            new(4, "Behavior collection", "PREPARED", true, false),
            new(5, "GridGuard AuditOnly", "PREPARED", false, false),
            new(6, "GridGuard Simulate", "PREPARED", false, false),
            new(7, "Evidence correlation", "PREPARED", false, false),
            new(8, "Candidate confirmation", "BLOCKED_BY_M22_EVIDENCE", true, false),
            new(9, "Rollback", "PREPARED", true, true)
        ]);
}

public static class VmWorkflowValidator
{
    private static readonly string[] Expected =
    [
        "Clean Snapshot",
        "Install target software",
        "Automatic snapshot",
        "Behavior collection",
        "GridGuard AuditOnly",
        "GridGuard Simulate",
        "Evidence correlation",
        "Candidate confirmation",
        "Rollback"
    ];

    public static IReadOnlyList<string> Validate(VmWorkflowPlan plan)
    {
        var errors = new List<string>();
        if (plan.RuntimeActive) errors.Add("Runtime must remain inactive.");
        if (plan.SafetyMode != "AuditOnly/Simulate")
            errors.Add("Only AuditOnly/Simulate is permitted.");
        if (!plan.Steps.Select(step => step.Name).SequenceEqual(Expected))
            errors.Add("Workflow order does not match the canonical VM workflow.");
        if (!plan.Steps.Select(step => step.Order)
            .SequenceEqual(Enumerable.Range(1, Expected.Length)))
            errors.Add("Workflow step order values must be contiguous and ordered.");
        if (plan.Steps.Any(step => step.MayMutateGuest && !step.RequiresHumanApproval))
            errors.Add("Every guest-mutating step requires human approval.");
        var installation = plan.Steps.SingleOrDefault(
            step => step.Name == "Install target software");
        if (installation is null || !installation.RequiresHumanApproval ||
            installation.State != "BLOCKED_BY_HUMAN_APPROVAL")
            errors.Add("Target installation must be blocked by human approval.");
        return errors;
    }
}

public sealed record SnapshotPair(
    InventorySnapshot Before,
    InventorySnapshot After,
    SnapshotDiff Difference);

public sealed class SnapshotOrchestrator(IInventoryAdapter inventory)
{
    public async Task<InventorySnapshot> CaptureAsync(
        CancellationToken cancellationToken = default) =>
        await inventory.CaptureAsync(cancellationToken);

    public static SnapshotPair Compare(
        InventorySnapshot before,
        InventorySnapshot after) =>
        new(before, after, SnapshotComparer.Compare(before, after));
}

public sealed record TypedDelta(
    string Kind,
    IReadOnlyList<InventoryRecord> Added,
    IReadOnlyList<InventoryRecord> Removed,
    IReadOnlyList<ChangedInventoryRecord> Changed);

public static class TypedDeltaEngine
{
    public static TypedDelta Compare(
        string kind,
        InventorySnapshot before,
        InventorySnapshot after)
    {
        var diff = SnapshotComparer.Compare(
            Filter(before, kind),
            Filter(after, kind));
        return new(kind, diff.Added, diff.Removed, diff.Changed);
    }

    private static InventorySnapshot Filter(InventorySnapshot snapshot, string kind) =>
        snapshot with
        {
            Records = snapshot.Records
                .Where(record => string.Equals(
                    record.Kind, kind, StringComparison.OrdinalIgnoreCase))
                .ToArray()
        };
}

public sealed record ArtifactIdentity(
    string Path,
    string Sha256,
    string PublisherStatus,
    string? Publisher,
    string? Product,
    string? Version,
    DateTimeOffset CollectedAt);

public interface IArtifactIdentityCollector
{
    Task<ArtifactIdentity?> CollectAsync(
        string path,
        CancellationToken cancellationToken = default);
}

public sealed class ArtifactIdentityCollector : IArtifactIdentityCollector
{
    public async Task<ArtifactIdentity?> CollectAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var normalized = path.Trim().Trim('"');
        if (!File.Exists(normalized)) return null;
        await using var stream = new FileStream(
            normalized, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var hash = Convert.ToHexString(
            await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        var status = "not-signed";
        string? publisher = null;
        try
        {
            using var certificate =
                new X509Certificate2(X509Certificate.CreateFromSignedFile(normalized));
            status = "signature-present-unverified-chain";
            publisher = certificate.Subject;
        }
        catch (CryptographicException)
        {
            // Absence of an embedded signature is evidence, not an error.
        }
        var version = FileVersionInfo.GetVersionInfo(normalized);
        return new(
            normalized,
            hash,
            status,
            publisher,
            version.ProductName,
            version.FileVersion,
            DateTimeOffset.UtcNow);
    }
}

public sealed record TimelineEntry(
    DateTimeOffset Timestamp,
    string Change,
    string Kind,
    string ObjectId);

public static class TimelineGenerator
{
    public static IReadOnlyList<TimelineEntry> Generate(SnapshotPair pair)
    {
        var entries = new List<TimelineEntry>();
        entries.AddRange(pair.Difference.Added.Select(record =>
            new TimelineEntry(pair.After.CapturedAt, "added", record.Kind, record.Id)));
        entries.AddRange(pair.Difference.Removed.Select(record =>
            new TimelineEntry(pair.After.CapturedAt, "removed", record.Kind, record.Id)));
        entries.AddRange(pair.Difference.Changed.Select(change =>
            new TimelineEntry(pair.After.CapturedAt, "changed", change.After.Kind, change.After.Id)));
        return entries.OrderBy(entry => entry.Timestamp)
            .ThenBy(entry => entry.Kind, StringComparer.Ordinal)
            .ThenBy(entry => entry.ObjectId, StringComparer.Ordinal)
            .ToArray();
    }
}

public sealed record ProcessTreeNode(
    string ProcessId,
    string? ParentProcessId,
    string Name,
    string? ExecutablePath);

public static class ProcessTreeRecorder
{
    public static IReadOnlyList<ProcessTreeNode> Record(InventorySnapshot snapshot) =>
        snapshot.Records
            .Where(record => record.Kind == "process")
            .Select(record => new ProcessTreeNode(
                record.Id,
                record.Properties.GetValueOrDefault("parentProcessId"),
                record.Properties.GetValueOrDefault("processName") ?? "",
                record.Properties.GetValueOrDefault("executablePath")))
            .OrderBy(node => node.ProcessId, StringComparer.Ordinal)
            .ToArray();
}

public sealed record CorrelationEdge(
    string FromKind,
    string FromId,
    string Relation,
    string ToKind,
    string ToId);

public static class EvidenceCorrelationEngine
{
    public static IReadOnlyList<CorrelationEdge> Correlate(InventorySnapshot snapshot)
    {
        var files = snapshot.Records.Where(record => record.Kind == "file").ToArray();
        var edges = new List<CorrelationEdge>();
        foreach (var record in snapshot.Records)
        {
            foreach (var property in new[]
            {
                "executablePath", "serviceImagePath", "commandLine", "path"
            })
            {
                if (!record.Properties.TryGetValue(property, out var value)) continue;
                var file = files.FirstOrDefault(candidate =>
                    value.Contains(candidate.Id, StringComparison.OrdinalIgnoreCase));
                if (file is not null && !(record.Kind == "file" && record.Id == file.Id))
                    edges.Add(new(
                        record.Kind, record.Id, property, "file", file.Id));
            }
        }
        foreach (var process in snapshot.Records.Where(record => record.Kind == "process"))
        {
            if (!process.Properties.TryGetValue("parentProcessId", out var parent) ||
                string.IsNullOrWhiteSpace(parent)) continue;
            edges.Add(new("process", parent, "parent-of", "process", process.Id));
        }
        return edges.Distinct().ToArray();
    }
}

public sealed record RuleReplayEvidence(
    string RuleId,
    string Decision,
    int Score,
    string Confidence,
    IReadOnlyList<string> AffectedObjects);

public sealed record VerificationEvidence(
    string Mode,
    string Status,
    bool Performed,
    string Detail);

public sealed record FalsePositiveFinding(
    string ObjectId,
    string Severity,
    string Rationale);

public static class FalsePositiveReviewWorkflow
{
    public static IReadOnlyList<FalsePositiveFinding> Review(
        IEnumerable<RuleReplayEvidence> matches,
        IEnumerable<ArtifactIdentity> identities)
    {
        var identityPaths = identities.Select(item => item.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return matches
            .Where(match => match.Decision is "Suspicious" or "Confirmed")
            .SelectMany(match => match.AffectedObjects.DefaultIfEmpty(match.RuleId))
            .Select(objectId => identityPaths.Contains(objectId)
                ? new FalsePositiveFinding(
                    objectId, "review", "Identity exists; verify publisher, product, and provenance.")
                : new FalsePositiveFinding(
                    objectId, "high", "No collected file identity; confirmation must fail closed."))
            .Distinct()
            .ToArray();
    }
}

public sealed record VmEvidencePackage(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    InventorySnapshot Before,
    InventorySnapshot After,
    IReadOnlyDictionary<string, TypedDelta> Deltas,
    IReadOnlyList<ArtifactIdentity> HashesPublishersVersions,
    IReadOnlyList<TimelineEntry> Timeline,
    IReadOnlyList<ProcessTreeNode> ProcessTree,
    IReadOnlyList<CorrelationEdge> CorrelationGraph,
    IReadOnlyList<RuleReplayEvidence> RuleMatches,
    IReadOnlyList<VerificationEvidence> SimulatedResponse,
    IReadOnlyList<FalsePositiveFinding> FalsePositiveReview,
    string SafetyMode,
    bool RuntimeActive);

public sealed class EvidenceCollector(IArtifactIdentityCollector identityCollector)
{
    private static readonly string[] DeltaKinds =
    [
        "process", "service", "registry", "autorun",
        "scheduledTask", "startupEntry", "file"
    ];

    public async Task<VmEvidencePackage> CollectAsync(
        InventorySnapshot before,
        InventorySnapshot after,
        IReadOnlyList<RuleReplayEvidence>? ruleMatches = null,
        IReadOnlyList<VerificationEvidence>? verification = null,
        CancellationToken cancellationToken = default)
    {
        var pair = SnapshotOrchestrator.Compare(before, after);
        var paths = pair.Difference.Added
            .Concat(pair.Difference.Changed.Select(change => change.After))
            .SelectMany(ArtifactPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var identities = new List<ArtifactIdentity>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var identity = await identityCollector.CollectAsync(path, cancellationToken);
            if (identity is not null) identities.Add(identity);
        }
        var matches = ruleMatches ?? [];
        return new(
            "1.0",
            DateTimeOffset.UtcNow,
            before,
            after,
            DeltaKinds.ToDictionary(
                kind => kind,
                kind => TypedDeltaEngine.Compare(kind, before, after)),
            identities,
            TimelineGenerator.Generate(pair),
            ProcessTreeRecorder.Record(after),
            EvidenceCorrelationEngine.Correlate(after),
            matches,
            verification ?? [],
            FalsePositiveReviewWorkflow.Review(matches, identities),
            "AuditOnly/Simulate",
            false);
    }

    private static IEnumerable<string> ArtifactPaths(InventoryRecord record)
    {
        foreach (var property in new[]
        {
            "path", "executablePath", "serviceImagePath", "commandLine"
        })
        {
            if (!record.Properties.TryGetValue(property, out var value)) continue;
            var candidate = ExtractExecutablePath(value);
            if (candidate is not null) yield return candidate;
        }
    }

    private static string? ExtractExecutablePath(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('"'))
        {
            var end = trimmed.IndexOf('"', 1);
            return end > 1 ? trimmed[1..end] : null;
        }
        var extension = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return extension >= 0 ? trimmed[..(extension + 4)] :
            File.Exists(trimmed) ? trimmed : null;
    }
}

public sealed record EvidencePackageOutput(string JsonPath, string MarkdownPath);

public static class EvidencePackageGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<EvidencePackageOutput> WriteAsync(
        VmEvidencePackage package,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        Directory.CreateDirectory(outputDirectory);
        var jsonPath = Path.Combine(outputDirectory, "evidence-package.json");
        var markdownPath = Path.Combine(outputDirectory, "evidence-package.md");
        await File.WriteAllTextAsync(
            jsonPath,
            JsonSerializer.Serialize(package, JsonOptions),
            cancellationToken);
        await File.WriteAllTextAsync(
            markdownPath,
            RenderMarkdown(package),
            cancellationToken);
        return new(jsonPath, markdownPath);
    }

    private static string RenderMarkdown(VmEvidencePackage package)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# GridGuard VM Evidence Package");
        builder.AppendLine();
        builder.AppendLine($"- Generated: `{package.GeneratedAt:O}`");
        builder.AppendLine($"- Safety mode: `{package.SafetyMode}`");
        builder.AppendLine($"- Runtime active: `{package.RuntimeActive}`");
        builder.AppendLine($"- Before records: {package.Before.Records.Count}");
        builder.AppendLine($"- After records: {package.After.Records.Count}");
        builder.AppendLine($"- Identities: {package.HashesPublishersVersions.Count}");
        builder.AppendLine($"- Correlation edges: {package.CorrelationGraph.Count}");
        builder.AppendLine($"- Rule matches: {package.RuleMatches.Count}");
        builder.AppendLine();
        builder.AppendLine("## Deltas");
        builder.AppendLine();
        foreach (var delta in package.Deltas.OrderBy(pair => pair.Key))
            builder.AppendLine(
                $"- {delta.Key}: +{delta.Value.Added.Count} " +
                $"-{delta.Value.Removed.Count} ~{delta.Value.Changed.Count}");
        builder.AppendLine();
        builder.AppendLine("## False-positive review");
        builder.AppendLine();
        foreach (var finding in package.FalsePositiveReview)
            builder.AppendLine(
                $"- `{finding.ObjectId}` [{finding.Severity}]: {finding.Rationale}");
        return builder.ToString();
    }
}
