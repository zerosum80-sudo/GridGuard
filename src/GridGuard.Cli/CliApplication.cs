using System.Text.Json;
using System.Text.Json.Serialization;
using GridGuard.Core;
using GridGuard.Detection;
using GridGuard.Monitoring;
using GridGuard.Rules;
using GridGuard.Response;

namespace GridGuard.Cli;

public static class CliApplication
{
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        IInventoryAdapter inventory,
        string repositoryRoot)
    {
        try
        {
            if (args is ["status"])
            {
                await output.WriteLineAsync("GridGuard mode: AuditOnly");
                await output.WriteLineAsync("Permanent deletion: unavailable");
                return 0;
            }

            if (args is ["scan"] or ["scan", "--mode", "audit"] or
                ["scan", "--mode", "simulate"])
            {
                var simulate = args is ["scan", "--mode", "simulate"];
                var rulePath = Path.Combine(repositoryRoot, "rules", "candidate");
                var rules = LoadRules(rulePath);
                var results = await new Scanner(inventory, new DetectionEngine()).ScanAsync(rules);
                if (results.Count == 0)
                {
                    await output.WriteLineAsync(
                        simulate
                            ? "No candidate match. Simulate made no changes."
                            : "No candidate match. AuditOnly made no changes.");
                    return 0;
                }
                var redactor = CurrentRedactor();
                foreach (var result in results)
                {
                    await output.WriteLineAsync(JsonSerializer.Serialize(Redact(result, redactor)));
                    if (!simulate) continue;
                    var paths = result.Evidence
                        .Where(item => item.Type is "executablePath" or "serviceImagePath")
                        .Select(item => CandidatePathParser.ExtractExecutablePath(item.Value))
                        .Where(path => path is not null)
                        .Cast<string>();
                    var outcomes = await new ResponseExecutor(
                        new(ResponseMode.Simulate, ExplicitlyEnabled: true),
                        new QuarantineStore(Path.Combine(repositoryRoot, "quarantine")))
                        .ExecuteAsync(result, paths);
                    foreach (var outcome in outcomes)
                        await output.WriteLineAsync(JsonSerializer.Serialize(outcome with
                        {
                            Detail = redactor.Redact(outcome.Detail)
                        }));
                }
                return results.Any(item => item.Decision == DetectionDecision.Confirmed) ? 20 : 10;
            }

            if (args is ["audit", "candidates", "--catalog", var catalogPath,
                "--output", var auditOutput])
            {
                EnsurePrivateOutput(repositoryRoot, auditOutput);
                var catalog = JsonSerializer.Deserialize<CandidateCatalog>(
                    await File.ReadAllTextAsync(catalogPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidDataException("Candidate catalog is empty.");
                var normalization = CandidateNormalizer.Normalize(catalog);
                var snapshot = await inventory.CaptureAsync();
                var report = await new CandidateAuditService(new MatchedFileInspector())
                    .AuditAsync(normalization, snapshot);
                var auditJsonOptions = new JsonSerializerOptions { WriteIndented = true };
                auditJsonOptions.Converters.Add(new JsonStringEnumConverter());
                await File.WriteAllTextAsync(
                    auditOutput,
                    JsonSerializer.Serialize(report, auditJsonOptions));
                var summary = new
                {
                    report.CapturedAt,
                    normalization.RowsReviewed,
                    normalization.ValuesReviewed,
                    normalization.DuplicatesRemoved,
                    normalization.MalformedRowsRemoved,
                    CandidateCount = normalization.Candidates.Count,
                    report.MatchCounts,
                    Correlations = report.Correlations,
                    InventoryErrorCount = report.InventoryErrors.Count,
                    InventoryErrorKinds = report.InventoryErrors
                        .Select(item => item.Split(':', 2)[0])
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    report.PromotionPolicy,
                    report.MutationStatus
                };
                await output.WriteLineAsync(JsonSerializer.Serialize(
                    summary,
                    new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            if (args is ["rules", "validate"])
            {
                var paths = Directory.EnumerateFiles(
                    Path.Combine(repositoryRoot, "rules"), "*.json", SearchOption.AllDirectories)
                    .Where(path =>
                        !path.Contains($"{Path.DirectorySeparatorChar}schema{Path.DirectorySeparatorChar}") &&
                        !path.EndsWith("synthetic-indicators.json"));
                var count = 0;
                foreach (var path in paths)
                {
                    RuleLoader.LoadFile(path);
                    count++;
                }
                await output.WriteLineAsync($"Validated {count} rules.");
                return 0;
            }

            if (args is ["rules", "list"])
            {
                foreach (var rule in LoadRules(Path.Combine(repositoryRoot, "rules", "candidate")))
                    await output.WriteLineAsync($"{rule.Id}\t{rule.Status}\t{rule.Confidence}");
                return 0;
            }

            if (args is ["rules", "explain", var id])
            {
                var rule = LoadRules(Path.Combine(repositoryRoot, "rules", "candidate"))
                    .SingleOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (rule is null) return 66;
                await output.WriteLineAsync($"{rule.Id}: {rule.Description} Score={rule.Score}");
                return 0;
            }

            if (args is ["quarantine", "list"])
            {
                var store = new QuarantineStore(Path.Combine(repositoryRoot, "quarantine"));
                foreach (var item in store.List())
                    await output.WriteLineAsync(
                        $"{item.Id}\t{item.RuleId}\t{item.OriginalPath}\t{item.QuarantinedAt:O}");
                return 0;
            }

            if (args is ["quarantine", "restore", var itemId])
            {
                await new QuarantineStore(Path.Combine(repositoryRoot, "quarantine"))
                    .RestoreAsync(itemId);
                await output.WriteLineAsync($"Restored quarantine item {itemId}.");
                return 0;
            }

            if (args is ["snapshot", "capture", "--output", var snapshotOutput])
            {
                var snapshot = await inventory.CaptureAsync();
                await File.WriteAllTextAsync(
                    snapshotOutput,
                    JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
                await output.WriteLineAsync($"Captured {snapshot.Records.Count} records.");
                return 0;
            }

            if (args is ["snapshot", "diff", var beforePath, var afterPath])
            {
                var before = JsonSerializer.Deserialize<InventorySnapshot>(
                    await File.ReadAllTextAsync(beforePath))!;
                var after = JsonSerializer.Deserialize<InventorySnapshot>(
                    await File.ReadAllTextAsync(afterPath))!;
                await output.WriteLineAsync(JsonSerializer.Serialize(
                    SnapshotComparer.Compare(before, after),
                    new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            if (args is ["diagnostics"])
            {
                await output.WriteLineAsync($"OS: {Environment.OSVersion}");
                await output.WriteLineAsync($".NET: {Environment.Version}");
                await output.WriteLineAsync("Mode: AuditOnly; permanent deletion unavailable");
                return 0;
            }

            await error.WriteLineAsync(
                "Usage: gridguard status|scan [--mode audit|simulate]|audit candidates ...|rules ...|quarantine ...|snapshot ...|diagnostics");
            return 64;
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        {
            await error.WriteLineAsync($"Failed action: {ex.Message}");
            return 70;
        }
    }

    private static GridRule[] LoadRules(string path) =>
        Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*.json")
                .Where(file => !file.EndsWith("synthetic-indicators.json"))
                .Select(RuleLoader.LoadFile).ToArray()
            : [];

    private static void EnsurePrivateOutput(string repositoryRoot, string outputPath)
    {
        var privateRoot = Path.GetFullPath(
            Path.Combine(repositoryRoot, "artifacts", "private-analysis")) +
            Path.DirectorySeparatorChar;
        var fullOutput = Path.GetFullPath(outputPath);
        if (!fullOutput.StartsWith(privateRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                "Candidate audit output must remain below artifacts/private-analysis.");
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutput)!);
    }

    private static PrivacyRedactor CurrentRedactor() => new(
        Environment.UserName,
        Environment.MachineName,
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    private static DetectionResult Redact(
        DetectionResult result,
        PrivacyRedactor redactor) =>
        result with
        {
            Evidence = result.Evidence.Select(item => item with
            {
                Value = redactor.Redact(item.Value),
                ObjectId = redactor.Redact(item.ObjectId),
                Metadata = item.Metadata?.ToDictionary(
                    pair => pair.Key,
                    pair => redactor.Redact(pair.Value))
            }).ToArray(),
            AffectedObjects = result.AffectedObjects.Select(redactor.Redact).ToArray()
        };
}
