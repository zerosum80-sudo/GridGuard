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
}
