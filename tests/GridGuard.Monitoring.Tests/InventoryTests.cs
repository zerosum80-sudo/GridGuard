using GridGuard.Monitoring;

namespace GridGuard.Monitoring.Tests;

public sealed class InventoryTests
{
    [Fact]
    public async Task CollectsSelectedTemporaryDirectoryWithoutMutation()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gridguard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "synthetic.txt");
        await File.WriteAllTextAsync(path, "fixture");
        try
        {
            var before = await File.ReadAllTextAsync(path);
            var snapshot = await new WindowsInventoryAdapter([root]).CaptureAsync();
            Assert.Contains(snapshot.Records, item => item.Kind == "file" && item.Id == path);
            Assert.Equal(before, await File.ReadAllTextAsync(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SnapshotDiffFindsAddedRemovedAndChanged()
    {
        var before = Snapshot(
            new("file", "a", new Dictionary<string, string> { ["size"] = "1" }),
            new("file", "b", new Dictionary<string, string> { ["size"] = "1" }));
        var after = Snapshot(
            new("file", "b", new Dictionary<string, string> { ["size"] = "2" }),
            new("file", "c", new Dictionary<string, string> { ["size"] = "1" }));
        var diff = SnapshotComparer.Compare(before, after);
        Assert.Single(diff.Added);
        Assert.Single(diff.Removed);
        Assert.Single(diff.Changed);
    }

    private static InventorySnapshot Snapshot(params InventoryRecord[] records) =>
        new(DateTimeOffset.UtcNow, records, []);

    [Fact]
    public void DeduplicatesInsideDebounceWindow()
    {
        var now = DateTimeOffset.UtcNow;
        var dedupe = new EventDeduplicator(TimeSpan.FromSeconds(1));
        Assert.True(dedupe.ShouldProcess(new("file", "x", now)));
        Assert.False(dedupe.ShouldProcess(new("file", "x", now.AddMilliseconds(50))));
        Assert.True(dedupe.ShouldProcess(new("file", "x", now.AddSeconds(2))));
    }

    [Fact]
    public async Task BoundedProcessorShutsDownGracefully()
    {
        var handled = new List<string>();
        var processor = new BoundedEventProcessor(
            2,
            new EventDeduplicator(TimeSpan.Zero),
            (item, _) => { handled.Add(item.ObjectId); return Task.CompletedTask; });
        processor.TryPublish(new("file", "a", DateTimeOffset.UtcNow));
        processor.TryPublish(new("file", "b", DateTimeOffset.UtcNow));
        processor.Complete();
        await processor.RunAsync(CancellationToken.None);
        Assert.Equal(["a", "b"], handled);
    }

    [Fact]
    public void GridComponentMonitorEmitsOnlyExactServiceAndProcessEvents()
    {
        var initial = new GridComponentState(
            true,
            @"C:\Program Files (x86)\NAT Service\natsvc.exe",
            "2",
            new HashSet<int> { 42 });

        var events = GridComponentEventSource.DetectChanges(null, initial);

        Assert.Contains(events, item =>
            item.Kind == "service-created" && item.ObjectId == "NATService");
        Assert.Contains(events, item =>
            item.Kind == "process-created" && item.ObjectId == "42");
        Assert.DoesNotContain(events, item =>
            item.ObjectId.Contains("Filebogo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GridComponentMonitorDetectsServiceStateChange()
    {
        var previous = new GridComponentState(
            true,
            @"C:\Program Files (x86)\NAT Service\natsvc.exe",
            "2",
            new HashSet<int>());
        var current = previous with { ProcessIds = new HashSet<int> { 99 } };

        var events = GridComponentEventSource.DetectChanges(previous, current);

        Assert.Contains(events, item => item.Kind == "service-state-changed");
        Assert.Contains(events, item =>
            item.Kind == "process-created" && item.ObjectId == "99");
    }
}
