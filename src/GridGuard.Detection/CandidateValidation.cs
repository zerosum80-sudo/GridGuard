using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using GridGuard.Monitoring;

namespace GridGuard.Detection;

public enum CandidateClassification
{
    ActionableCandidate,
    SupportingCandidate,
    GenericRuntime,
    VendorApplication,
    PotentialGridComponent,
    PersistenceIndicator,
    RemovalException,
    Malformed,
    Duplicate,
    Unresolved
}

public sealed record CandidateCatalog(CandidateCatalogRow[] Indicators);

public sealed record CandidateCatalogRow(
    string? ProcessName,
    string? ServiceName,
    string? RunCurrentUserValue,
    string? RunLocalMachineValue);

public sealed record NormalizedCandidate(
    string Type,
    string Value,
    CandidateClassification Classification,
    string ComponentRole,
    bool CandidateRuleSuitable,
    int Occurrences);

public sealed record CandidateNormalizationResult(
    int RowsReviewed,
    int ValuesReviewed,
    int DuplicatesRemoved,
    int MalformedRowsRemoved,
    IReadOnlyList<NormalizedCandidate> Candidates);

public static partial class CandidateNormalizer
{
    private static readonly HashSet<string> GenericNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "BigService.exe", "ExpressService.exe", "FileService.exe", "KService.exe",
        "Respon.exe", "SvcEnv.exe", "TaskSvc.exe", "VManager.exe"
    };

    public static CandidateNormalizationResult Normalize(CandidateCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var values = new List<(string Type, string Value)>();
        var malformedRows = 0;
        foreach (var row in catalog.Indicators ?? [])
        {
            var process = NormalizeExecutable(row.ProcessName);
            var service = NormalizeValue(row.ServiceName);
            var runCurrentUser = NormalizeValue(row.RunCurrentUserValue);
            var runLocalMachine = NormalizeValue(row.RunLocalMachineValue);
            if (process is null && service is null &&
                runCurrentUser is null && runLocalMachine is null)
            {
                malformedRows++;
                continue;
            }
            Add(values, "executableName", process);
            Add(values, "serviceName", service);
            Add(values, "startupEntry", runCurrentUser);
            Add(values, "startupEntry", runLocalMachine);
        }

        var normalized = values
            .GroupBy(item => $"{item.Type}\0{item.Value}", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var (classification, role, suitable) = Classify(first.Type, first.Value);
                return new NormalizedCandidate(
                    first.Type, first.Value, classification, role, suitable, group.Count());
            })
            .OrderBy(item => item.Type, StringComparer.Ordinal)
            .ThenBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new(
            catalog.Indicators?.Length ?? 0,
            values.Count,
            values.Count - normalized.Length,
            malformedRows,
            normalized);
    }

    public static string? NormalizeExecutable(string? value)
    {
        var normalized = NormalizeValue(value);
        if (normalized is null) return null;
        normalized = normalized.Replace('/', '\\');
        normalized = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(Path.GetExtension(normalized)))
            normalized += ".exe";
        return normalized;
    }

    public static string NormalizeRegistryPath(string value) =>
        value.Trim().Trim('"').Replace('/', '\\')
            .Replace("HKLM\\", "HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase)
            .Replace("HKCU\\", "HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = Whitespace().Replace(value.Trim().Trim('"'), " ");
        return normalized.Length == 0 ? null : normalized;
    }

    private static void Add(List<(string Type, string Value)> values, string type, string? value)
    {
        if (value is not null) values.Add((type, value));
    }

    private static (CandidateClassification Classification, string Role, bool Suitable)
        Classify(string type, string value)
    {
        if (type == "startupEntry")
            return (CandidateClassification.PersistenceIndicator, "autorun component", true);
        if (GenericNames.Contains(value))
            return (CandidateClassification.GenericRuntime, "ambiguous executable", false);
        if (value.Contains("grid", StringComparison.OrdinalIgnoreCase))
            return (CandidateClassification.PotentialGridComponent, "grid/P2P component", true);
        if (value.Contains("update", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("launcher", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("manager", StringComparison.OrdinalIgnoreCase))
            return (CandidateClassification.VendorApplication, "updater or client application", false);
        if (type == "serviceName")
            return (CandidateClassification.SupportingCandidate, "service", true);
        return (CandidateClassification.ActionableCandidate, "removal-table executable", true);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

public sealed record MatchedFileMetadata(
    string Path,
    string Sha256,
    string SignatureStatus,
    string? Signer,
    string? ProductName,
    string? FileVersion);

public interface IMatchedFileInspector
{
    Task<MatchedFileMetadata?> InspectAsync(
        string path,
        CancellationToken cancellationToken = default);
}

public sealed class MatchedFileInspector : IMatchedFileInspector
{
    public async Task<MatchedFileMetadata?> InspectAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var executablePath = CandidatePathParser.ExtractExecutablePath(path);
        if (executablePath is null || !File.Exists(executablePath)) return null;
        await using var stream = new FileStream(
            executablePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var hash = Convert.ToHexString(
            await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        string signatureStatus;
        string? signer = null;
        try
        {
            using var certificate =
                new X509Certificate2(X509Certificate.CreateFromSignedFile(executablePath));
            signatureStatus = "signature-present-unverified-chain";
            signer = certificate.Subject;
        }
        catch (CryptographicException)
        {
            signatureStatus = "not-signed";
        }
        var version = FileVersionInfo.GetVersionInfo(executablePath);
        return new(
            executablePath,
            hash,
            signatureStatus,
            signer,
            version.ProductName,
            version.FileVersion);
    }
}

public static partial class CandidatePathParser
{
    public static string? ExtractExecutablePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var expanded = Environment.ExpandEnvironmentVariables(value.Trim());
        var match = ExecutablePath().Match(expanded);
        if (!match.Success) return null;
        return match.Groups["path"].Value.Trim('"').Replace('/', '\\');
    }

    [GeneratedRegex("(?<path>\"[^\"]+\\.exe\"|[^\"\\r\\n]*?\\.exe)", RegexOptions.IgnoreCase)]
    private static partial Regex ExecutablePath();
}

public sealed record CandidateMatch(
    string CandidateType,
    string CandidateValue,
    CandidateClassification Classification,
    string RecordKind,
    string RecordId,
    IReadOnlyDictionary<string, string> Evidence,
    MatchedFileMetadata? File);

public sealed record CandidateCorrelation(
    string ExecutableName,
    string[] EvidenceKinds,
    string Confidence,
    string Rationale);

public sealed record CandidateAuditReport(
    DateTimeOffset CapturedAt,
    CandidateNormalizationResult Normalization,
    IReadOnlyList<CandidateMatch> Matches,
    IReadOnlyList<CandidateCorrelation> Correlations,
    IReadOnlyDictionary<string, int> MatchCounts,
    string PromotionPolicy,
    string MutationStatus,
    IReadOnlyList<string> InventoryErrors);

public sealed class CandidateAuditService(IMatchedFileInspector fileInspector)
{
    public async Task<CandidateAuditReport> AuditAsync(
        CandidateNormalizationResult normalization,
        InventorySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var matches = new List<CandidateMatch>();
        foreach (var record in snapshot.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var candidate in normalization.Candidates)
            {
                if (!TryMatch(candidate, record, out var matchedPath)) continue;
                var selectedEvidence = record.Properties
                    .Where(pair => RelevantProperties.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                var file = matchedPath is null
                    ? null
                    : await fileInspector.InspectAsync(matchedPath, cancellationToken);
                matches.Add(new(
                    candidate.Type,
                    candidate.Value,
                    candidate.Classification,
                    record.Kind,
                    record.Id,
                    selectedEvidence,
                    file));
            }
        }

        var correlations = matches
            .Select(match => new
            {
                Match = match,
                Executable = MatchExecutableName(match)
            })
            .Where(item => item.Executable is not null)
            .GroupBy(item => item.Executable!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var kinds = group.Select(item => item.Match.RecordKind)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var candidateTypes = group.Select(item => item.Match.CandidateType)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                var hasFile = group.Any(item => item.Match.File is not null);
                var strong = (kinds.Length >= 2 || candidateTypes >= 2) && hasFile;
                return new CandidateCorrelation(
                    group.Key,
                    kinds,
                    strong ? "StrongCandidate" : "WeakCandidate",
                    strong
                        ? "Multiple current-system evidence kinds correlate with an existing hashed file; independent product confirmation is still absent."
                        : "A current object matched, but cross-source correlation or an existing file hash is absent.");
            })
            .ToArray();
        var counts = matches.GroupBy(match => match.RecordKind)
            .ToDictionary(group => group.Key, group => group.Count());
        return new(
            snapshot.CapturedAt,
            normalization,
            matches,
            correlations,
            counts,
            "Automatic Confirmed promotion is prohibited; recommend only after two independent non-circular sources.",
            "AuditOnly; no process, file, service, registry, task, or quarantine mutation.",
            snapshot.Errors);
    }

    private static readonly HashSet<string> RelevantProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "processName", "executablePath", "serviceName", "serviceDisplayName",
            "serviceImagePath", "serviceStartType", "serviceState", "registryPath",
            "entryName", "commandLine"
        };

    private static bool TryMatch(
        NormalizedCandidate candidate,
        InventoryRecord record,
        out string? matchedPath)
    {
        matchedPath = null;
        if (candidate.Type == "executableName")
        {
            if (record.Properties.TryGetValue("executablePath", out var processPath) &&
                FileNameEquals(processPath, candidate.Value))
            {
                matchedPath = processPath;
                return true;
            }
            if (record.Properties.TryGetValue("serviceImagePath", out var servicePath) &&
                FileNameEquals(servicePath, candidate.Value))
            {
                matchedPath = servicePath;
                return true;
            }
            if (record.Properties.TryGetValue("commandLine", out var commandLine) &&
                FileNameEquals(commandLine, candidate.Value))
            {
                matchedPath = commandLine;
                return true;
            }
            if (record.Properties.TryGetValue("processName", out var processName) &&
                string.Equals(
                    Path.GetFileNameWithoutExtension(candidate.Value),
                    Path.GetFileNameWithoutExtension(processName),
                    StringComparison.OrdinalIgnoreCase))
                return true;
        }
        if (candidate.Type == "serviceName" &&
            (PropertyEquals(record, "serviceName", candidate.Value) ||
             PropertyEquals(record, "serviceDisplayName", candidate.Value)))
        {
            record.Properties.TryGetValue("serviceImagePath", out matchedPath);
            return true;
        }
        if (candidate.Type == "startupEntry" &&
            PropertyEquals(record, "entryName", candidate.Value))
        {
            record.Properties.TryGetValue("commandLine", out matchedPath);
            return true;
        }
        return false;
    }

    private static bool PropertyEquals(
        InventoryRecord record,
        string property,
        string expected) =>
        record.Properties.TryGetValue(property, out var actual) &&
        string.Equals(actual.Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private static bool FileNameEquals(string path, string expected)
    {
        var parsed = CandidatePathParser.ExtractExecutablePath(path);
        return parsed is not null &&
            string.Equals(Path.GetFileName(parsed), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string? MatchExecutableName(CandidateMatch match)
    {
        if (match.CandidateType == "executableName") return match.CandidateValue;
        var path = match.File?.Path ??
            match.Evidence.GetValueOrDefault("executablePath") ??
            match.Evidence.GetValueOrDefault("serviceImagePath") ??
            match.Evidence.GetValueOrDefault("commandLine");
        var parsed = CandidatePathParser.ExtractExecutablePath(path);
        return parsed is null ? null : Path.GetFileName(parsed);
    }
}

public static class CandidatePromotionPolicy
{
    public sealed record EvidenceSource(
        string SourceId,
        string ControlId,
        bool IsPrimary,
        bool IsCandidateSpecific,
        bool HasReproducibleIdentity,
        bool IsCircular);

    public sealed record Evaluation(
        string Recommendation,
        int QualifyingSourceCount,
        IReadOnlyList<string> Reasons);

    public static Evaluation Evaluate(
        CandidateClassification classification,
        IEnumerable<EvidenceSource> sources,
        bool hasPlausibleGenericInterpretation)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var reasons = new List<string>();
        if (classification is CandidateClassification.GenericRuntime or
            CandidateClassification.RemovalException)
            return new("Excluded", 0, ["Candidate classification is excluded."]);
        if (classification == CandidateClassification.VendorApplication)
            return new("VendorApplication", 0, ["Candidate is a vendor application."]);

        var qualifying = sources
            .Where(source =>
                !string.IsNullOrWhiteSpace(source.SourceId) &&
                !string.IsNullOrWhiteSpace(source.ControlId) &&
                source.IsPrimary &&
                source.IsCandidateSpecific &&
                source.HasReproducibleIdentity &&
                !source.IsCircular)
            .GroupBy(source => source.ControlId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        if (hasPlausibleGenericInterpretation)
            reasons.Add("A plausible generic interpretation remains.");
        if (qualifying.Length < 2)
            reasons.Add("Fewer than two independently controlled qualifying sources.");

        return qualifying.Length >= 2 && !hasPlausibleGenericInterpretation
            ? new("RecommendConfirmationReview", qualifying.Length,
                ["Two independent primary sources establish reproducible candidate identity."])
            : new(
                qualifying.Length >= 1 ? "StrongCandidate" : "Unresolved",
                qualifying.Length,
                reasons);
    }

    public static string Recommend(
        CandidateClassification classification,
        int independentNonCircularSources,
        bool hasPlausibleGenericInterpretation)
    {
        if (classification is CandidateClassification.GenericRuntime or
            CandidateClassification.RemovalException)
            return "Excluded";
        if (classification == CandidateClassification.VendorApplication)
            return "VendorApplication";
        if (independentNonCircularSources >= 2 && !hasPlausibleGenericInterpretation)
            return "RecommendConfirmationReview";
        return independentNonCircularSources >= 1 ? "StrongCandidate" : "Unresolved";
    }
}

public sealed class PrivacyRedactor(
    string userName,
    string machineName,
    string userProfile)
{
    public string Redact(string value)
    {
        var redacted = value;
        if (!string.IsNullOrWhiteSpace(userProfile))
            redacted = redacted.Replace(
                userProfile, "<USER_PROFILE>", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(userName))
            redacted = redacted.Replace(
                userName, "<USER>", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(machineName))
            redacted = redacted.Replace(
                machineName, "<HOST>", StringComparison.OrdinalIgnoreCase);
        return redacted;
    }
}
