namespace GridGuard.Rules;

public static class RuleValidator
{
    private static readonly HashSet<string> Confidences =
        ["hypothesis", "candidate", "strong-inference", "confirmed"];
    private static readonly HashSet<string> Statuses = ["enabled", "disabled", "candidate"];
    private static readonly HashSet<string> Operators =
        ["equalsIgnoreCase", "containsIgnoreCase", "endsWithIgnoreCase", "equals", "sha256Equals"];

    public static RuleValidationResult Validate(GridRule rule)
    {
        var errors = new List<string>();
        if (rule.SchemaVersion != "1.0") errors.Add("schemaVersion must be 1.0.");
        if (string.IsNullOrWhiteSpace(rule.Id)) errors.Add("id is required.");
        if (string.IsNullOrWhiteSpace(rule.Name)) errors.Add("name is required.");
        if (!Confidences.Contains(rule.Confidence)) errors.Add("confidence is invalid.");
        if (!Statuses.Contains(rule.Status)) errors.Add("status is invalid.");
        if (rule.Sources.Length == 0) errors.Add("at least one source is required.");
        if (rule.Score is < 0 or > 100) errors.Add("score must be between 0 and 100.");
        if (rule.Response.PermanentDelete) errors.Add("permanentDelete is unsupported.");
        ValidateExpression(rule.Match, "match", errors);
        for (var i = 0; i < rule.Exclusions.Length; i++)
            ValidateExpression(rule.Exclusions[i], $"exclusions[{i}]", errors);
        if (rule.Status == "candidate" && rule.Confidence == "confirmed")
            errors.Add("candidate rules cannot claim confirmed confidence.");
        ValidateConfirmation(rule, errors);
        return new RuleValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateConfirmation(GridRule rule, List<string> errors)
    {
        if (rule.Confidence != "confirmed")
        {
            if (rule.Confirmation is not null)
                errors.Add("confirmation evidence is allowed only for confirmed rules.");
            return;
        }
        if (rule.Confirmation is null)
        {
            errors.Add("confirmed rules require structured confirmation evidence.");
            return;
        }
        if (rule.Confirmation.Policy != "independent-primary-v1")
            errors.Add("confirmation policy must be independent-primary-v1.");
        var sources = rule.Confirmation.Sources
            .Where(source =>
                !string.IsNullOrWhiteSpace(source.SourceId) &&
                !string.IsNullOrWhiteSpace(source.ControlId) &&
                Uri.TryCreate(source.Uri, UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                !string.IsNullOrWhiteSpace(source.Identity))
            .ToArray();
        if (sources.Length != rule.Confirmation.Sources.Length)
            errors.Add("confirmation sources require id, control, HTTPS URI, and identity.");
        if (sources.Select(source => source.ControlId)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
            errors.Add("confirmed rules require two independently controlled sources.");
    }

    private static void ValidateExpression(MatchExpression expression, string path, List<string> errors)
    {
        var forms = new[]
        {
            expression.Type is not null || expression.Operator is not null || expression.Value is not null,
            expression.All is not null,
            expression.Any is not null,
            expression.Threshold is not null
        }.Count(x => x);
        if (forms != 1) errors.Add($"{path} must contain exactly one expression form.");

        if (expression.Type is not null || expression.Operator is not null || expression.Value is not null)
        {
            if (string.IsNullOrWhiteSpace(expression.Type) ||
                string.IsNullOrWhiteSpace(expression.Value) ||
                expression.Operator is null || !Operators.Contains(expression.Operator))
                errors.Add($"{path} leaf is incomplete or has an unsupported operator.");
        }

        ValidateChildren(expression.All, path + ".all", errors);
        ValidateChildren(expression.Any, path + ".any", errors);
        ValidateChildren(expression.Threshold, path + ".threshold", errors);
        if (expression.Threshold is not null &&
            (expression.Minimum is null || expression.Minimum < 1 ||
             expression.Minimum > expression.Threshold.Length))
            errors.Add($"{path}.minimum is outside the threshold child range.");
    }

    private static void ValidateChildren(
        MatchExpression[]? children,
        string path,
        List<string> errors)
    {
        if (children is null) return;
        if (children.Length == 0) errors.Add($"{path} cannot be empty.");
        for (var i = 0; i < children.Length; i++)
            ValidateExpression(children[i], $"{path}[{i}]", errors);
    }
}
