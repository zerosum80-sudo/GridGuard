namespace GridGuard.RuleCompiler;

public sealed record RawIndicator(
    string Type,
    string Value,
    string SourceType,
    string SourceLocation,
    string ExtractionMethod,
    string Confidence,
    bool IndependentlyVerified);

public sealed record CandidateIndicator(
    string Type,
    string Value,
    string SourceType,
    string SourceLocation,
    string ExtractionMethod,
    string Confidence,
    bool IndependentlyVerified,
    string RuleStatus,
    string ConfirmationNote);

public static class IndicatorNormalizer
{
    private static readonly HashSet<string> SupportedTypes =
    [
        "processName", "executablePath", "sha256", "publisher", "certificateThumbprint",
        "serviceName", "serviceDisplayName", "serviceImagePath", "scheduledTaskPath",
        "scheduledTaskAction", "registryPath", "startupEntry", "directoryLayout",
        "parentChild", "commandLine", "fileCoOccurrence", "persistenceCoOccurrence"
    ];

    public static CandidateIndicator Normalize(RawIndicator raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (!SupportedTypes.Contains(raw.Type))
            throw new ArgumentException($"Unsupported indicator type: {raw.Type}");
        if (string.IsNullOrWhiteSpace(raw.Value))
            throw new ArgumentException("Indicator value cannot be empty.");
        if (string.IsNullOrWhiteSpace(raw.SourceType) ||
            string.IsNullOrWhiteSpace(raw.SourceLocation) ||
            string.IsNullOrWhiteSpace(raw.ExtractionMethod))
            throw new ArgumentException("Indicator provenance is required.");

        var confidence = raw.Confidence switch
        {
            "hypothesis" => "hypothesis",
            "observation" => "observation",
            "strong-inference" => "strong-inference",
            _ => throw new ArgumentException($"Unsupported candidate confidence: {raw.Confidence}")
        };

        var value = raw.Type switch
        {
            "sha256" => NormalizeHex(raw.Value, 64),
            "certificateThumbprint" => NormalizeHex(raw.Value, null),
            "executablePath" or "serviceImagePath" or "registryPath" or "directoryLayout"
                => raw.Value.Trim().Replace('/', '\\'),
            _ => raw.Value.Trim()
        };

        return new CandidateIndicator(
            raw.Type,
            value,
            raw.SourceType.Trim(),
            raw.SourceLocation.Trim(),
            raw.ExtractionMethod.Trim(),
            confidence,
            raw.IndependentlyVerified,
            "candidate",
            raw.IndependentlyVerified
                ? "Independent verification recorded; contextual rule review is still required."
                : "Extracted observation is not independently verified and cannot be confirmed.");
    }

    private static string NormalizeHex(string value, int? requiredLength)
    {
        var normalized = value.Replace(" ", "").Replace(":", "").Trim().ToLowerInvariant();
        if (requiredLength is not null && normalized.Length != requiredLength)
            throw new ArgumentException($"Hex indicator must contain {requiredLength} characters.");
        if (normalized.Length == 0 || normalized.Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("Hex indicator contains invalid characters.");
        return normalized;
    }
}

