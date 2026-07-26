using System.Text.Json;
using GridGuard.Monitoring;

var options = new JsonSerializerOptions { WriteIndented = true };
if (args is ["capture", "--output", var output])
{
    var snapshot = await new WindowsInventoryAdapter().CaptureAsync();
    await File.WriteAllTextAsync(output, JsonSerializer.Serialize(snapshot, options));
    Console.WriteLine($"Captured {snapshot.Records.Count} records.");
    return 0;
}

if (args is ["diff", var beforePath, var afterPath])
{
    var before = JsonSerializer.Deserialize<InventorySnapshot>(
        await File.ReadAllTextAsync(beforePath), options)
        ?? throw new InvalidDataException("Before snapshot is empty.");
    var after = JsonSerializer.Deserialize<InventorySnapshot>(
        await File.ReadAllTextAsync(afterPath), options)
        ?? throw new InvalidDataException("After snapshot is empty.");
    Console.WriteLine(JsonSerializer.Serialize(SnapshotComparer.Compare(before, after), options));
    return 0;
}

Console.Error.WriteLine(
    "Usage: gridguard-snapshot capture --output <file> | diff <before.json> <after.json>");
return 64;

