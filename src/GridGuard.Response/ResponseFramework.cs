using System.Security.Cryptography;
using System.Text.Json;
using GridGuard.Core;

namespace GridGuard.Response;

public enum ResponseMode { AuditOnly, Simulate, Quarantine, Remediate }

public sealed record ResponseConfiguration(
    ResponseMode Mode = ResponseMode.AuditOnly,
    bool ExplicitlyEnabled = false,
    bool AllowProcessTermination = false,
    bool AllowFileQuarantine = false,
    bool AllowServiceChanges = false,
    bool AllowPersistenceChanges = false,
    int ConfirmedScoreThreshold = 100);

public sealed record ResponseOutcome(
    string Action,
    bool Performed,
    string Status,
    string Detail);

public static class ResponseConfigurationValidator
{
    public static IReadOnlyList<string> Validate(ResponseConfiguration configuration)
    {
        var errors = new List<string>();
        if (configuration.Mode != ResponseMode.AuditOnly && !configuration.ExplicitlyEnabled)
            errors.Add("Non-audit modes require ExplicitlyEnabled=true.");
        if (configuration.ConfirmedScoreThreshold is < 1 or > 100)
            errors.Add("ConfirmedScoreThreshold must be between 1 and 100.");
        if (configuration.Mode == ResponseMode.AuditOnly &&
            (configuration.AllowProcessTermination || configuration.AllowFileQuarantine ||
             configuration.AllowServiceChanges || configuration.AllowPersistenceChanges))
            errors.Add("AuditOnly cannot allow host mutation.");
        return errors;
    }
}

public sealed record QuarantineRecord(
    string Id,
    string OriginalPath,
    string QuarantinePath,
    string Sha256,
    string RuleId,
    DateTimeOffset QuarantinedAt);

public sealed class QuarantineStore(string root)
{
    public async Task<QuarantineRecord> QuarantineAsync(
        string sourcePath,
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var id = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(root);
        var itemPath = Path.Combine(root, id + ".bin");
        File.Move(sourcePath, itemPath);
        var record = new QuarantineRecord(
            id, Path.GetFullPath(sourcePath), itemPath, hash, ruleId, DateTimeOffset.UtcNow);
        await File.WriteAllTextAsync(
            Path.Combine(root, id + ".json"),
            JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
        return record;
    }

    public IEnumerable<QuarantineRecord> List() =>
        Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.json")
                .Select(path => JsonSerializer.Deserialize<QuarantineRecord>(File.ReadAllText(path))!)
            : [];

    public async Task RestoreAsync(string id, CancellationToken cancellationToken = default)
    {
        var metadataPath = Path.Combine(root, id + ".json");
        var record = JsonSerializer.Deserialize<QuarantineRecord>(
            await File.ReadAllTextAsync(metadataPath, cancellationToken))
            ?? throw new InvalidDataException("Quarantine metadata is invalid.");
        var bytes = await File.ReadAllBytesAsync(record.QuarantinePath, cancellationToken);
        var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (actual != record.Sha256) throw new InvalidDataException("Quarantine hash mismatch.");
        if (File.Exists(record.OriginalPath))
            throw new IOException("Restore target already exists.");
        Directory.CreateDirectory(Path.GetDirectoryName(record.OriginalPath)!);
        File.Move(record.QuarantinePath, record.OriginalPath);
        File.Delete(metadataPath);
    }
}

public sealed class ResponseExecutor(
    ResponseConfiguration configuration,
    QuarantineStore quarantine)
{
    public async Task<IReadOnlyList<ResponseOutcome>> ExecuteAsync(
        DetectionResult detection,
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        var errors = ResponseConfigurationValidator.Validate(configuration);
        if (errors.Count > 0)
            return [new("configuration", false, "failed", string.Join(" ", errors))];
        if (detection.Decision != DetectionDecision.Confirmed ||
            detection.Score < configuration.ConfirmedScoreThreshold)
            return [new("response", false, "observation-only",
                "Only confirmed detections meeting the score threshold may mutate the host.")];
        if (configuration.Mode == ResponseMode.AuditOnly)
            return [new("response", false, "audit-only", "No system modification performed.")];
        if (configuration.Mode == ResponseMode.Simulate)
            return filePaths.Select(path =>
                new ResponseOutcome("quarantine", false, "simulated", path)).ToArray();
        if (configuration.Mode == ResponseMode.Remediate)
            return [new("remediate", false, "failed",
                "Real process/service/persistence adapters are disabled in this baseline.")];
        if (!configuration.AllowFileQuarantine)
            return [new("quarantine", false, "failed", "File quarantine flag is disabled.")];

        var outcomes = new List<ResponseOutcome>();
        foreach (var path in filePaths)
        {
            var record = await quarantine.QuarantineAsync(path, detection.RuleId, cancellationToken);
            outcomes.Add(new("quarantine", true, "performed", record.Id));
        }
        return outcomes;
    }
}

