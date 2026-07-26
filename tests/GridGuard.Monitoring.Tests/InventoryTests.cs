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
}

