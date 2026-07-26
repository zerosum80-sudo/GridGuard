using GridGuard.Cli;
using GridGuard.Monitoring;

namespace GridGuard.IntegrationTests;

public sealed class CliTests
{
    [Fact]
    public async Task StatusConfirmsAuditOnly()
    {
        var output = new StringWriter();
        var code = await CliApplication.RunAsync(
            ["status"], output, TextWriter.Null, new FakeInventory(), ".");
        Assert.Equal(0, code);
        Assert.Contains("AuditOnly", output.ToString());
    }

    [Fact]
    public async Task InvalidCommandReturnsUsageExitCode()
    {
        var code = await CliApplication.RunAsync(
            ["unknown"], TextWriter.Null, TextWriter.Null, new FakeInventory(), ".");
        Assert.Equal(64, code);
    }

    private sealed class FakeInventory : IInventoryAdapter
    {
        public Task<InventorySnapshot> CaptureAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new InventorySnapshot(DateTimeOffset.UtcNow, [], []));
    }
}

