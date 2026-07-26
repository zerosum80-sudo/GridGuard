using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace GridGuard.Monitoring;

public sealed record MonitoringEvent(
    string Kind,
    string ObjectId,
    DateTimeOffset ObservedAt,
    IReadOnlyDictionary<string, string>? Properties = null);

public sealed class EventDeduplicator(TimeSpan window)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new();

    public bool ShouldProcess(MonitoringEvent item)
    {
        var key = $"{item.Kind}\0{item.ObjectId}".ToUpperInvariant();
        var accepted = false;
        _seen.AddOrUpdate(
            key,
            _ => { accepted = true; return item.ObservedAt; },
            (_, previous) =>
            {
                if (item.ObservedAt - previous >= window) accepted = true;
                return accepted ? item.ObservedAt : previous;
            });
        return accepted;
    }
}

public sealed class BoundedEventProcessor(
    int capacity,
    EventDeduplicator deduplicator,
    Func<MonitoringEvent, CancellationToken, Task> handler)
{
    private readonly Channel<MonitoringEvent> _channel =
        Channel.CreateBounded<MonitoringEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryPublish(MonitoringEvent item) => _channel.Writer.TryWrite(item);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
            if (deduplicator.ShouldProcess(item))
                await handler(item, cancellationToken);
    }

    public void Complete() => _channel.Writer.TryComplete();
}

public sealed class ProcessPollingSource(TimeSpan interval)
{
    public async Task RunAsync(
        Action<MonitoringEvent> publish,
        CancellationToken cancellationToken)
    {
        var known = new HashSet<int>();
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var current = Process.GetProcesses().Select(process =>
            {
                using (process) return (process.Id, process.ProcessName);
            }).ToArray();
            foreach (var process in current.Where(item => !known.Contains(item.Id)))
                publish(new("process-start", process.Id.ToString(), DateTimeOffset.UtcNow,
                    new Dictionary<string, string> { ["processName"] = process.ProcessName }));
            known = current.Select(item => item.Id).ToHashSet();
        }
    }
}

public sealed class FileSystemEventSource(IEnumerable<string> roots) : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = [];

    public void Start(Action<MonitoringEvent> publish)
    {
        foreach (var root in roots.Where(Directory.Exists))
        {
            var watcher = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                               NotifyFilters.CreationTime | NotifyFilters.Size
            };
            FileSystemEventHandler handler = (_, item) =>
                publish(new("file-change", item.FullPath, DateTimeOffset.UtcNow,
                    new Dictionary<string, string> { ["changeType"] = item.ChangeType.ToString() }));
            RenamedEventHandler renamed = (_, item) =>
                publish(new("file-rename", item.FullPath, DateTimeOffset.UtcNow,
                    new Dictionary<string, string> { ["oldPath"] = item.OldFullPath }));
            watcher.Created += handler;
            watcher.Changed += handler;
            watcher.Deleted += handler;
            watcher.Renamed += renamed;
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers) watcher.Dispose();
        _watchers.Clear();
    }
}

public sealed class ReconciliationLoop(
    IInventoryAdapter inventory,
    TimeSpan interval,
    Func<InventorySnapshot, CancellationToken, Task> handler)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await handler(await inventory.CaptureAsync(cancellationToken), cancellationToken);
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
            await handler(await inventory.CaptureAsync(cancellationToken), cancellationToken);
    }
}
