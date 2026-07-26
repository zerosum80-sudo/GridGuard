namespace GridGuard.Monitoring;

public sealed record SnapshotDiff(
    IReadOnlyList<InventoryRecord> Added,
    IReadOnlyList<InventoryRecord> Removed,
    IReadOnlyList<ChangedInventoryRecord> Changed);

public sealed record ChangedInventoryRecord(
    InventoryRecord Before,
    InventoryRecord After);

public static class SnapshotComparer
{
    public static SnapshotDiff Compare(InventorySnapshot before, InventorySnapshot after)
    {
        var oldItems = before.Records.ToDictionary(Key);
        var newItems = after.Records.ToDictionary(Key);
        var added = newItems.Where(pair => !oldItems.ContainsKey(pair.Key))
            .Select(pair => pair.Value).ToArray();
        var removed = oldItems.Where(pair => !newItems.ContainsKey(pair.Key))
            .Select(pair => pair.Value).ToArray();
        var changed = oldItems.Keys.Intersect(newItems.Keys)
            .Where(key => !Equivalent(oldItems[key], newItems[key]))
            .Select(key => new ChangedInventoryRecord(oldItems[key], newItems[key]))
            .ToArray();
        return new SnapshotDiff(added, removed, changed);
    }

    private static string Key(InventoryRecord record) =>
        $"{record.Kind}\0{record.Id}".ToUpperInvariant();

    private static bool Equivalent(InventoryRecord left, InventoryRecord right) =>
        left.Properties.Count == right.Properties.Count &&
        left.Properties.All(pair =>
            right.Properties.TryGetValue(pair.Key, out var value) && value == pair.Value);
}

