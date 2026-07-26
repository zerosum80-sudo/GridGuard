using GridGuard.Core;
using GridGuard.Monitoring;
using GridGuard.Rules;

namespace GridGuard.Detection;

public sealed class Scanner(IInventoryAdapter inventory, DetectionEngine engine)
{
    public async Task<IReadOnlyList<DetectionResult>> ScanAsync(
        IEnumerable<GridRule> rules,
        IEnumerable<GridRule>? allowlist = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await inventory.CaptureAsync(cancellationToken);
        var evidence = snapshot.Records.SelectMany(record =>
            record.Properties.Select(pair =>
                new EvidenceItem(pair.Key, pair.Value, record.Id)));
        return rules.Where(rule => rule.Status != "disabled")
            .Select(rule => engine.Evaluate(rule, evidence, allowlist))
            .Where(result => result.Decision != DetectionDecision.Clean)
            .ToArray();
    }
}

