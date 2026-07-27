using GridGuard.Core;
using GridGuard.Detection;
using GridGuard.Rules;

namespace GridGuard.Detection.Tests;

public sealed class DetectionEngineTests
{
    [Fact]
    public void AllExpressionProducesSuspiciousCandidate()
    {
        var result = new DetectionEngine().Evaluate(
            Rule(new()
            {
                All =
                [
                    Leaf("serviceName", "Synthetic"),
                    Leaf("serviceImagePath", @"C:\SyntheticGrid\agent.exe", "containsIgnoreCase")
                ]
            }),
            [
                new("serviceName", "synthetic", "svc"),
                new("serviceImagePath", @"C:\SyntheticGrid\agent.exe", "svc")
            ]);
        Assert.Equal(DetectionDecision.Suspicious, result.Decision);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void ThresholdRequiresMinimum()
    {
        var rule = Rule(new()
        {
            Threshold = [Leaf("serviceName", "a"), Leaf("processName", "b")],
            Minimum = 2
        });
        var result = new DetectionEngine().Evaluate(
            rule, [new("serviceName", "a", "svc")]);
        Assert.Equal(DetectionDecision.Clean, result.Decision);
    }

    [Fact]
    public void ExclusionWins()
    {
        var rule = Rule(Leaf("serviceName", "a")) with
        {
            Exclusions = [Leaf("publisher", "trusted")]
        };
        var result = new DetectionEngine().Evaluate(
            rule, [new("serviceName", "a", "svc"), new("publisher", "trusted", "svc")]);
        Assert.Equal(DetectionDecision.Clean, result.Decision);
    }

    [Fact]
    public void AllowlistHasHighestPrecedence()
    {
        var rule = Rule(Leaf("serviceName", "a"));
        var allow = Rule(Leaf("publisher", "trusted")) with
        {
            Id = "allow.synthetic",
            Status = "enabled"
        };
        var result = new DetectionEngine().Evaluate(
            rule,
            [new("serviceName", "a", "svc"), new("publisher", "trusted", "svc")],
            [allow]);
        Assert.Equal(DetectionDecision.Allowlisted, result.Decision);
    }

    [Fact]
    public void NormalizesPathsHashesAndThumbprints()
    {
        Assert.Equal(
            @"C:\Temp\x.exe",
            EvidenceNormalizer.Normalize(new("executablePath", "\"C:/Temp/x.exe\"", "x")).Value);
        Assert.Equal(
            "aabb",
            EvidenceNormalizer.Normalize(new("certificateThumbprint", "AA:BB", "x")).Value);
    }

    [Fact]
    public void MatchedResultContainsOnlyRuleRelevantEvidence()
    {
        var result = new DetectionEngine().Evaluate(
            Rule(Leaf("serviceName", "target")),
            [
                new("serviceName", "target", "target-service"),
                new("processName", "unrelated", "private-process")
            ]);
        Assert.Single(result.Evidence);
        Assert.DoesNotContain(result.Evidence, item => item.ObjectId == "private-process");
    }

    [Fact]
    public void EndsWithIgnoreCaseRequiresExactPathSuffix()
    {
        var rule = Rule(new()
        {
            All =
            [
                Leaf("serviceName", "NATService"),
                Leaf(
                    "serviceImagePath",
                    @"\NAT Service\natsvc.exe",
                    "endsWithIgnoreCase")
            ]
        });
        var match = new DetectionEngine().Evaluate(
            rule,
            [
                new("serviceName", "natservice", "NATService"),
                new(
                    "serviceImagePath",
                    @"""C:\Program Files (x86)\NAT Service\natsvc.exe""",
                    "NATService")
            ]);
        var mismatch = new DetectionEngine().Evaluate(
            rule,
            [
                new("serviceName", "NATService", "NATService"),
                new(
                    "serviceImagePath",
                    @"C:\Program Files (x86)\NAT Service\natsvc.exe.backup",
                    "NATService")
            ]);

        Assert.Equal(DetectionDecision.Suspicious, match.Decision);
        Assert.Equal(DetectionDecision.Clean, mismatch.Decision);
    }

    private static MatchExpression Leaf(string type, string value, string op = "equalsIgnoreCase") =>
        new() { Type = type, Operator = op, Value = value };

    private static GridRule Rule(MatchExpression match) => new()
    {
        SchemaVersion = "1.0",
        Id = "synthetic",
        Name = "Synthetic",
        Vendor = "synthetic",
        Family = "test",
        Description = "test",
        Confidence = "candidate",
        Status = "enabled",
        Sources = ["synthetic"],
        Match = match,
        Score = 100,
        Response = new(false, false, false, false, false, false),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
