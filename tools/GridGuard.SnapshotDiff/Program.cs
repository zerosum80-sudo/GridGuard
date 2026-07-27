using System.Text.Json;
using GridGuard.Monitoring;

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
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

if (args is ["workflow", "validate"])
{
    var plan = VmWorkflowPlanner.Create();
    var errors = VmWorkflowValidator.Validate(plan);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        status = errors.Count == 0 ? "PASS" : "FAIL",
        plan,
        errors
    }, options));
    return errors.Count == 0 ? 0 : 1;
}

if (args is ["hypervisors", "inspect"])
{
    var status = HypervisorAbstraction.Inspect(
        HypervisorAbstraction.CreateDetectedAdapters());
    Console.WriteLine(JsonSerializer.Serialize(status, options));
    return 0;
}

if (args is ["evidence", var evidenceBefore, var evidenceAfter, "--output", var directory])
{
    var before = JsonSerializer.Deserialize<InventorySnapshot>(
        await File.ReadAllTextAsync(evidenceBefore), options)
        ?? throw new InvalidDataException("Before snapshot is empty.");
    var after = JsonSerializer.Deserialize<InventorySnapshot>(
        await File.ReadAllTextAsync(evidenceAfter), options)
        ?? throw new InvalidDataException("After snapshot is empty.");
    var package = await new EvidenceCollector(new ArtifactIdentityCollector())
        .CollectAsync(before, after);
    var evidenceOutput = await EvidencePackageGenerator.WriteAsync(package, directory);
    Console.WriteLine(JsonSerializer.Serialize(evidenceOutput, options));
    return 0;
}

Console.Error.WriteLine(
    "Usage: gridguard-snapshot capture --output <file> | " +
    "diff <before.json> <after.json> | workflow validate | hypervisors inspect | " +
    "evidence <before.json> <after.json> --output <directory>");
return 64;
