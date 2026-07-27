using GridGuard.Core;
using GridGuard.Rules;

namespace GridGuard.Detection;

public sealed class DetectionEngine
{
    public DetectionResult Evaluate(
        GridRule rule,
        IEnumerable<EvidenceItem> evidence,
        IEnumerable<GridRule>? allowlist = null)
    {
        var normalized = evidence.Select(EvidenceNormalizer.Normalize).ToArray();
        var allowed = allowlist?.FirstOrDefault(item =>
            item.Status == "enabled" && EvaluateExpression(item.Match, normalized));
        if (allowed is not null)
            return Result(rule, SelectMatchedEvidence(allowed.Match, normalized), 0,
                DetectionDecision.Allowlisted,
                $"Allowlist rule {allowed.Id} takes precedence.", "No action.");

        if (rule.Status == "disabled")
            return Result(rule, normalized, 0, DetectionDecision.Clean,
                "Rule is disabled.", "No action.");

        if (rule.Exclusions.Any(exclusion => EvaluateExpression(exclusion, normalized)))
            return Result(rule, normalized, 0, DetectionDecision.Clean,
                "An exclusion matched.", "No action.");

        if (!EvaluateExpression(rule.Match, normalized))
            return Result(rule, normalized, 0, DetectionDecision.Clean,
                "Required evidence did not match.", "No action.");

        var matched = SelectMatchedEvidence(rule.Match, normalized);
        var decision = rule.Confidence == "confirmed"
            ? DetectionDecision.Confirmed
            : DetectionDecision.Suspicious;
        return Result(rule, matched, rule.Score, decision,
            $"Rule {rule.Id} matched with {rule.Confidence} confidence.",
            decision == DetectionDecision.Confirmed
                ? "Apply the configured guarded response."
                : "Observe and seek independent verification.");
    }

    private static bool EvaluateExpression(
        MatchExpression expression,
        IReadOnlyList<EvidenceItem> evidence)
    {
        if (expression.All is not null)
            return expression.All.All(item => EvaluateExpression(item, evidence));
        if (expression.Any is not null)
            return expression.Any.Any(item => EvaluateExpression(item, evidence));
        if (expression.Threshold is not null)
            return expression.Threshold.Count(item => EvaluateExpression(item, evidence))
                   >= expression.Minimum.GetValueOrDefault();

        return evidence.Any(item =>
            string.Equals(item.Type, expression.Type, StringComparison.OrdinalIgnoreCase) &&
            MatchValue(item.Value, expression.Operator!, expression.Value!));
    }

    private static bool MatchValue(string actual, string op, string expected) => op switch
    {
        "equalsIgnoreCase" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        "containsIgnoreCase" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
        "equals" => actual == expected,
        "sha256Equals" => string.Equals(
            actual.Replace(" ", ""), expected.Replace(" ", ""), StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    private static IReadOnlyList<EvidenceItem> SelectMatchedEvidence(
        MatchExpression expression,
        IReadOnlyList<EvidenceItem> evidence)
    {
        var leaves = EnumerateLeaves(expression).ToArray();
        return evidence.Where(item => leaves.Any(leaf =>
                string.Equals(item.Type, leaf.Type, StringComparison.OrdinalIgnoreCase) &&
                MatchValue(item.Value, leaf.Operator!, leaf.Value!)))
            .Distinct()
            .ToArray();
    }

    private static IEnumerable<MatchExpression> EnumerateLeaves(MatchExpression expression)
    {
        if (expression.Type is not null)
        {
            yield return expression;
            yield break;
        }
        foreach (var child in expression.All ?? expression.Any ?? expression.Threshold ?? [])
            foreach (var leaf in EnumerateLeaves(child))
                yield return leaf;
    }

    private static DetectionResult Result(
        GridRule rule,
        IReadOnlyList<EvidenceItem> evidence,
        int score,
        DetectionDecision decision,
        string explanation,
        string response) => new(
            rule.Id,
            evidence,
            rule.Confidence,
            score,
            decision,
            explanation,
            DateTimeOffset.UtcNow,
            evidence.Select(item => item.ObjectId).Distinct().ToArray(),
            response);
}
