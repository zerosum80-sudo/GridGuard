using GridGuard.Core;
using GridGuard.Monitoring;
using GridGuard.Rules;

namespace GridGuard.Detection;

public sealed class DetectionReplayEngine(DetectionEngine detection)
{
    public IReadOnlyList<RuleReplayEvidence> Replay(
        IEnumerable<GridRule> rules,
        InventorySnapshot snapshot,
        IEnumerable<ArtifactIdentity>? identities = null,
        IEnumerable<GridRule>? allowlist = null)
    {
        var evidence = snapshot.Records.SelectMany(record =>
            record.Properties.Select(property =>
                new EvidenceItem(
                    property.Key,
                    property.Value,
                    record.Id,
                    new Dictionary<string, string>
                    {
                        ["recordKind"] = record.Kind
                    })))
            .Concat((identities ?? []).Select(item =>
                new EvidenceItem("sha256", item.Sha256, item.Path)))
            .ToArray();
        return rules.Select(rule => detection.Evaluate(rule, evidence, allowlist))
            .Where(result => result.Decision is
                DetectionDecision.Suspicious or DetectionDecision.Confirmed)
            .Select(result => new RuleReplayEvidence(
                result.RuleId,
                result.Decision.ToString(),
                result.Score,
                result.Confidence,
                result.AffectedObjects))
            .ToArray();
    }
}
