using GridGuard.Core;
using GridGuard.Response;

namespace GridGuard.Response.Tests;

public sealed class ResponseTests
{
    [Fact]
    public async Task AuditOnlyDoesNotMoveFile()
    {
        var (root, path) = await FixtureAsync();
        try
        {
            var outcome = await new ResponseExecutor(
                new(), new(Path.Combine(root, "q"))).ExecuteAsync(Confirmed(), [path]);
            Assert.False(outcome.Single().Performed);
            Assert.True(File.Exists(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SimulateProducesPlanWithoutMove()
    {
        var (root, path) = await FixtureAsync();
        try
        {
            var outcome = await new ResponseExecutor(
                new(ResponseMode.Simulate, true), new(Path.Combine(root, "q")))
                .ExecuteAsync(Confirmed(), [path]);
            Assert.Equal("simulated", outcome.Single().Status);
            Assert.True(File.Exists(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SimulateCandidateProducesObservationPlanWithoutMove()
    {
        var (root, path) = await FixtureAsync();
        try
        {
            var candidate = Confirmed() with
            {
                Confidence = "strong-inference",
                Decision = DetectionDecision.Suspicious
            };
            var outcome = await new ResponseExecutor(
                new(ResponseMode.Simulate, true), new(Path.Combine(root, "q")))
                .ExecuteAsync(candidate, [path]);
            Assert.Equal("simulated", outcome.Single().Status);
            Assert.Equal("response-plan", outcome.Single().Action);
            Assert.Contains("no quarantine", outcome.Single().Detail);
            Assert.False(outcome.Single().Performed);
            Assert.True(File.Exists(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QuarantineRecordsAndRestores()
    {
        var (root, path) = await FixtureAsync();
        try
        {
            var store = new QuarantineStore(Path.Combine(root, "q"));
            var outcome = await new ResponseExecutor(
                new(ResponseMode.Quarantine, true, AllowFileQuarantine: true), store)
                .ExecuteAsync(Confirmed(), [path]);
            Assert.True(outcome.Single().Performed);
            Assert.False(File.Exists(path));
            var record = Assert.Single(store.List());
            await store.RestoreAsync(record.Id);
            Assert.True(File.Exists(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void RejectsUnsafeAuditConfiguration()
    {
        Assert.NotEmpty(ResponseConfigurationValidator.Validate(
            new(ResponseMode.AuditOnly, AllowProcessTermination: true)));
    }

    [Fact]
    public async Task VmVerificationRunsAuditAndSimulateWithoutMutation()
    {
        var (root, path) = await FixtureAsync();
        try
        {
            var evidence = await VmSafeVerificationWorkflow.VerifyAsync(
                Confirmed(),
                [path],
                Path.Combine(root, "unused-quarantine"));

            Assert.Contains(evidence, item => item.Mode == "AuditOnly");
            Assert.Contains(evidence, item => item.Mode == "Simulate");
            Assert.All(evidence, item => Assert.False(item.Performed));
            Assert.True(File.Exists(path));
            Assert.False(Directory.Exists(Path.Combine(root, "unused-quarantine")));
        }
        finally { Directory.Delete(root, true); }
    }

    private static DetectionResult Confirmed() => new(
        "synthetic", [], "confirmed", 100, DetectionDecision.Confirmed, "synthetic",
        DateTimeOffset.UtcNow, [], "quarantine");

    private static async Task<(string Root, string Path)> FixtureAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gridguard-response-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "fixture.bin");
        await File.WriteAllTextAsync(path, "synthetic");
        return (root, path);
    }
}
