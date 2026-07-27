using GridGuard.Detection;
using GridGuard.Monitoring;
using GridGuard.Rules;

namespace GridGuard.Detection.Tests;

public sealed class DetectionReplayTests
{
    [Fact]
    public void ReplaysSnapshotEvidenceWithoutMutation()
    {
        var properties = new Dictionary<string, string>
        {
            ["serviceName"] = "SyntheticGridService",
            ["serviceImagePath"] = @"C:\Synthetic\grid.exe"
        };
        var snapshot = new InventorySnapshot(
            DateTimeOffset.UtcNow,
            [new("service", "fixture", properties)],
            []);
        var rule = new GridRule
        {
            SchemaVersion = "1.0",
            Id = "synthetic-replay",
            Name = "Synthetic replay",
            Vendor = "synthetic",
            Family = "test",
            Description = "synthetic",
            Confidence = "candidate",
            Status = "candidate",
            Sources = ["synthetic"],
            Match = new()
            {
                Type = "serviceName",
                Operator = "equalsIgnoreCase",
                Value = "SyntheticGridService"
            },
            Score = 50,
            Response = new(false, false, false, false, false, false),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var results = new DetectionReplayEngine(new DetectionEngine())
            .Replay([rule], snapshot);

        Assert.Equal("Suspicious", Assert.Single(results).Decision);
        Assert.Equal("SyntheticGridService", properties["serviceName"]);
    }
}
