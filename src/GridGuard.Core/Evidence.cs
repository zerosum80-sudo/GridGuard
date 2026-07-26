namespace GridGuard.Core;

public sealed record EvidenceItem(
    string Type,
    string Value,
    string ObjectId,
    IReadOnlyDictionary<string, string>? Metadata = null);

public static class EvidenceNormalizer
{
    public static EvidenceItem Normalize(EvidenceItem evidence) => evidence with
    {
        Type = evidence.Type.Trim(),
        Value = evidence.Type switch
        {
            "executablePath" or "serviceImagePath" or "directoryLayout"
                => NormalizePath(evidence.Value),
            "sha256" or "certificateThumbprint"
                => evidence.Value.Replace(" ", "").Replace(":", "").ToLowerInvariant(),
            _ => evidence.Value.Trim()
        },
        ObjectId = evidence.ObjectId.Trim()
    };

    public static string NormalizePath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return expanded.Replace('/', '\\').TrimEnd('\\');
    }
}

public enum DetectionDecision
{
    Clean,
    Observe,
    Suspicious,
    Confirmed,
    Allowlisted
}

public sealed record DetectionResult(
    string RuleId,
    IReadOnlyList<EvidenceItem> Evidence,
    string Confidence,
    int Score,
    DetectionDecision Decision,
    string Explanation,
    DateTimeOffset Timestamp,
    IReadOnlyList<string> AffectedObjects,
    string RecommendedResponse);

