using System.Text.Json;
using GridGuard.Monitoring;

namespace GridGuard.Monitoring.Tests;

public sealed class VmPreparationTests
{
    [Fact]
    public void WorkflowIsCanonicalAndStopsAtApprovalBoundary()
    {
        var plan = VmWorkflowPlanner.Create();
        Assert.Empty(VmWorkflowValidator.Validate(plan));
        Assert.False(plan.RuntimeActive);
        Assert.Equal(
            "BLOCKED_BY_HUMAN_APPROVAL",
            plan.Steps.Single(step => step.Name == "Install target software").State);
    }

    [Fact]
    public void HypervisorAbstractionSupportsAllProvidersAndReadyState()
    {
        IVmHypervisorAdapter[] adapters =
        [
            new HyperVPlanningAdapter(),
            new VMwarePlanningAdapter(),
            new VirtualBoxPlanningAdapter()
        ];
        var status = HypervisorAbstraction.Inspect(adapters);

        Assert.Equal("READY_FOR_VM", status.State);
        Assert.Equal(3, status.Supported.Count);
        Assert.Empty(status.Available);
        Assert.All(adapters, adapter =>
        {
            Assert.True(adapter.PlanSnapshot("fixture-vm", "clean").RequiresHumanApproval);
            Assert.True(adapter.PlanRollback("fixture-vm", "clean").RequiresHumanApproval);
        });
    }

    [Theory]
    [InlineData("process")]
    [InlineData("service")]
    [InlineData("registry")]
    [InlineData("autorun")]
    [InlineData("scheduledTask")]
    [InlineData("startupEntry")]
    [InlineData("file")]
    public void TypedDeltaEnginesSeparateEvidenceKinds(string kind)
    {
        var before = Snapshot(new InventoryRecord(
            kind, "old", new Dictionary<string, string> { ["value"] = "1" }));
        var after = Snapshot(new InventoryRecord(
            kind, "new", new Dictionary<string, string> { ["value"] = "2" }));

        var delta = TypedDeltaEngine.Compare(kind, before, after);

        Assert.Single(delta.Added);
        Assert.Single(delta.Removed);
        Assert.Empty(delta.Changed);
    }

    [Fact]
    public async Task EvidenceCollectorBuildsTimelineTreeGraphAndDualFormatPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gridguard-vm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var executable = Path.Combine(root, "synthetic.exe");
            await File.WriteAllTextAsync(executable, "synthetic evidence only");
            var before = Snapshot();
            var after = Snapshot(
                new("file", executable, new Dictionary<string, string>
                {
                    ["path"] = executable,
                    ["size"] = new FileInfo(executable).Length.ToString()
                }),
                new("process", "20", new Dictionary<string, string>
                {
                    ["processName"] = "synthetic",
                    ["executablePath"] = executable,
                    ["parentProcessId"] = "10"
                }),
                new("process", "10", new Dictionary<string, string>
                {
                    ["processName"] = "parent",
                    ["executablePath"] = "",
                    ["parentProcessId"] = "1"
                }),
                new("service", "fixture-service", new Dictionary<string, string>
                {
                    ["serviceImagePath"] = executable
                }));
            var matches = new[]
            {
                new RuleReplayEvidence(
                    "fixture-rule", "Suspicious", 50, "candidate", [executable])
            };
            var verification = new[]
            {
                new VerificationEvidence(
                    "Simulate", "simulated", false, "No host modification.")
            };

            var package = await new EvidenceCollector(new ArtifactIdentityCollector())
                .CollectAsync(before, after, matches, verification);
            var output = await EvidencePackageGenerator.WriteAsync(
                package, Path.Combine(root, "evidence"));

            Assert.False(package.RuntimeActive);
            Assert.Equal("AuditOnly/Simulate", package.SafetyMode);
            Assert.NotEmpty(package.Timeline);
            Assert.Equal(2, package.ProcessTree.Count);
            Assert.Contains(package.CorrelationGraph, edge =>
                edge.FromKind == "process" && edge.ToKind == "file");
            Assert.Single(package.HashesPublishersVersions);
            Assert.Single(package.RuleMatches);
            Assert.Single(package.SimulatedResponse);
            Assert.True(File.Exists(output.JsonPath));
            Assert.True(File.Exists(output.MarkdownPath));
            var roundTrip = JsonSerializer.Deserialize<VmEvidencePackage>(
                await File.ReadAllTextAsync(output.JsonPath));
            Assert.NotNull(roundTrip);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void FalsePositiveReviewFailsClosedWithoutIdentity()
    {
        var review = FalsePositiveReviewWorkflow.Review(
            [new("candidate", "Suspicious", 70, "strong-inference", ["missing.exe"])],
            []);
        Assert.Equal("high", Assert.Single(review).Severity);
    }

    private static InventorySnapshot Snapshot(params InventoryRecord[] records) =>
        new(DateTimeOffset.UtcNow, records, []);
}
