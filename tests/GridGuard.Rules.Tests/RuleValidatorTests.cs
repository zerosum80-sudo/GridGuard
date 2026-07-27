using GridGuard.Rules;

namespace GridGuard.Rules.Tests;

public sealed class RuleValidatorTests
{
    [Fact]
    public void LoadsSyntheticRule()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "rules", "candidate", "grid.synthetic.001.json"));
        var rule = RuleLoader.LoadFile(path);
        Assert.Equal("candidate", rule.Status);
        Assert.False(rule.Response.PermanentDelete);
    }

    [Fact]
    public void LoadsNatServiceCandidateWithNonMutatingResponse()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "rules", "candidate", "grid.natservice.001.json"));
        var rule = RuleLoader.LoadFile(path);

        Assert.Equal("candidate", rule.Status);
        Assert.Equal("strong-inference", rule.Confidence);
        Assert.Equal(60, rule.Score);
        Assert.Contains(
            rule.Match.All!,
            expression => expression.Operator == "endsWithIgnoreCase");
        Assert.Equal(
            new(false, false, false, false, false, false),
            rule.Response);
        Assert.Null(rule.Confirmation);
    }

    [Fact]
    public void RejectsPermanentDeletion()
    {
        var rule = ValidRule() with
        {
            Response = new(false, false, false, false, false, true)
        };
        Assert.Contains(
            RuleValidator.Validate(rule).Errors,
            error => error.Contains("permanentDelete"));
    }

    [Fact]
    public void RejectsInvalidThreshold()
    {
        var rule = ValidRule() with
        {
            Match = new()
            {
                Threshold = [new() { Type = "serviceName", Operator = "equals", Value = "x" }],
                Minimum = 2
            }
        };
        Assert.False(RuleValidator.Validate(rule).IsValid);
    }

    [Fact]
    public void RejectsConfirmedRuleWithoutIndependentStructuredEvidence()
    {
        var rule = ValidRule() with
        {
            Confidence = "confirmed",
            Status = "enabled",
            Confirmation = new()
            {
                Policy = "independent-primary-v1",
                Sources =
                [
                    new()
                    {
                        SourceId = "vendor-page",
                        ControlId = "vendor",
                        Uri = "https://vendor.invalid/product",
                        Identity = "name and version"
                    },
                    new()
                    {
                        SourceId = "vendor-mirror",
                        ControlId = "vendor",
                        Uri = "https://mirror.invalid/product",
                        Identity = "same publisher"
                    }
                ]
            }
        };
        Assert.Contains(
            RuleValidator.Validate(rule).Errors,
            error => error.Contains("independently controlled"));
    }

    [Fact]
    public void AcceptsConfirmedRuleWithTwoIndependentStructuredSources()
    {
        var rule = ValidRule() with
        {
            Confidence = "confirmed",
            Status = "enabled",
            Confirmation = new()
            {
                Policy = "independent-primary-v1",
                Sources =
                [
                    new()
                    {
                        SourceId = "vendor-page",
                        ControlId = "vendor",
                        Uri = "https://vendor.invalid/product",
                        Identity = "publisher, product, file, version"
                    },
                    new()
                    {
                        SourceId = "security-advisory",
                        ControlId = "security-lab",
                        Uri = "https://security.invalid/advisory",
                        Identity = "file hash and signer"
                    }
                ]
            }
        };
        Assert.True(RuleValidator.Validate(rule).IsValid);
    }

    private static GridRule ValidRule() => new()
    {
        SchemaVersion = "1.0",
        Id = "synthetic",
        Name = "Synthetic",
        Vendor = "synthetic",
        Family = "test",
        Description = "test",
        Confidence = "candidate",
        Status = "candidate",
        Sources = ["synthetic"],
        Match = new() { Type = "serviceName", Operator = "equals", Value = "x" },
        Score = 1,
        Response = new(false, false, false, false, false, false),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
