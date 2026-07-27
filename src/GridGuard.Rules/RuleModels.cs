using System.Text.Json;
using System.Text.Json.Serialization;

namespace GridGuard.Rules;

public sealed record GridRule
{
    public required string SchemaVersion { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Vendor { get; init; }
    public required string Family { get; init; }
    public required string Description { get; init; }
    public required string Confidence { get; init; }
    public required string Status { get; init; }
    public required string[] Sources { get; init; }
    public ConfirmationEvidence? Confirmation { get; init; }
    public required MatchExpression Match { get; init; }
    public MatchExpression[] Exclusions { get; init; } = [];
    public required int Score { get; init; }
    public required RuleResponse Response { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record ConfirmationEvidence
{
    public required string Policy { get; init; }
    public required ConfirmationSource[] Sources { get; init; }
}

public sealed record ConfirmationSource
{
    public required string SourceId { get; init; }
    public required string ControlId { get; init; }
    public required string Uri { get; init; }
    public required string Identity { get; init; }
}

public sealed record MatchExpression
{
    public string? Type { get; init; }
    public string? Operator { get; init; }
    public string? Value { get; init; }
    public MatchExpression[]? All { get; init; }
    public MatchExpression[]? Any { get; init; }
    public MatchExpression[]? Threshold { get; init; }
    public int? Minimum { get; init; }
    public int Weight { get; init; } = 1;
}

public sealed record RuleResponse(
    bool TerminateProcess,
    bool StopService,
    bool DisableService,
    bool RemovePersistence,
    bool QuarantineFiles,
    bool PermanentDelete);

public sealed record RuleValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class RuleLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static GridRule Load(string json)
    {
        var rule = JsonSerializer.Deserialize<GridRule>(json, Options)
            ?? throw new JsonException("Rule document is empty.");
        var validation = RuleValidator.Validate(rule);
        if (!validation.IsValid)
            throw new InvalidDataException(string.Join(Environment.NewLine, validation.Errors));
        return rule;
    }

    public static GridRule LoadFile(string path) => Load(File.ReadAllText(path));
}
