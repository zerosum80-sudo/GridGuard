using GridGuard.Core;
using GridGuard.Detection;
using GridGuard.Monitoring;
using GridGuard.Response;
using GridGuard.Rules;

namespace GridGuard.IntegrationTests;

public sealed class SyntheticEndToEndTests
{
    [Fact]
    public async Task DetectSimulateQuarantineAndRestoreSyntheticFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gridguard-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "SyntheticGrid", "agent.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await File.WriteAllTextAsync(file, "synthetic-grid-fixture");
        try
        {
            var rule = ConfirmedSyntheticRule();
            var scanner = new Scanner(new FakeInventory(file), new DetectionEngine());
            var detection = Assert.Single(await scanner.ScanAsync([rule]));
            Assert.Equal(DetectionDecision.Confirmed, detection.Decision);

            var store = new QuarantineStore(Path.Combine(root, "quarantine"));
            var simulated = await new ResponseExecutor(
                new(ResponseMode.Simulate, true), store).ExecuteAsync(detection, [file]);
            Assert.Equal("simulated", simulated.Single().Status);
            Assert.True(File.Exists(file));

            var performed = await new ResponseExecutor(
                new(ResponseMode.Quarantine, true, AllowFileQuarantine: true), store)
                .ExecuteAsync(detection, [file]);
            Assert.True(performed.Single().Performed);
            Assert.False(File.Exists(file));
            var record = Assert.Single(store.List());
            await store.RestoreAsync(record.Id);
            Assert.True(File.Exists(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SuspiciousCandidateCannotQuarantine()
    {
        var result = new DetectionResult(
            "candidate", [], "candidate", 100, DetectionDecision.Suspicious, "synthetic",
            DateTimeOffset.UtcNow, [], "observe");
        var outcome = await new ResponseExecutor(
            new(ResponseMode.Quarantine, true, AllowFileQuarantine: true),
            new QuarantineStore(Path.GetTempPath())).ExecuteAsync(result, []);
        Assert.Equal("observation-only", outcome.Single().Status);
    }

    private static GridRule ConfirmedSyntheticRule() => new()
    {
        SchemaVersion = "1.0",
        Id = "grid.synthetic.confirmed",
        Name = "Synthetic confirmed fixture",
        Vendor = "synthetic",
        Family = "test-fixture",
        Description = "Test only",
        Confidence = "confirmed",
        Status = "enabled",
        Sources = ["integration-test"],
        Match = new()
        {
            All =
            [
                new() { Type = "serviceName", Operator = "equalsIgnoreCase", Value = "SyntheticGridService" },
                new() { Type = "serviceImagePath", Operator = "containsIgnoreCase", Value = @"\SyntheticGrid\" }
            ]
        },
        Score = 100,
        Response = new(false, false, false, false, true, false),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private sealed class FakeInventory(string file) : IInventoryAdapter
    {
        public Task<InventorySnapshot> CaptureAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new InventorySnapshot(
                DateTimeOffset.UtcNow,
                [new("service", "SyntheticGridService", new Dictionary<string, string>
                {
                    ["serviceName"] = "SyntheticGridService",
                    ["serviceImagePath"] = file
                })],
                []));
    }
}

