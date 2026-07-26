using System.Text.Json;
using GridGuard.Core;
using GridGuard.Detection;
using GridGuard.Monitoring;
using GridGuard.Rules;

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

            if (args is ["scan"] or ["scan", "--mode", "audit"])
            {
                var rulePath = Path.Combine(repositoryRoot, "rules", "candidate");
                var rules = LoadRules(rulePath);
                var results = await new Scanner(inventory, new DetectionEngine()).ScanAsync(rules);
                if (results.Count == 0)
                {
                    await output.WriteLineAsync("No threat found. AuditOnly made no changes.");
                    return 0;
                }
                foreach (var result in results)
                    await output.WriteLineAsync(JsonSerializer.Serialize(result));
                return results.Any(item => item.Decision == DetectionDecision.Confirmed) ? 20 : 10;
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

            await error.WriteLineAsync(
                "Usage: gridguard status|scan|rules validate|rules list|rules explain <id>");
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
}
