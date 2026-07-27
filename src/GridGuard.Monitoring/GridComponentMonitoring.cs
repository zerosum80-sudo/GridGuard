using System.Diagnostics;
using Microsoft.Win32;

namespace GridGuard.Monitoring;

public sealed record GridComponentState(
    bool ServicePresent,
    string ServiceImagePath,
    string ServiceStartType,
    IReadOnlySet<int> ProcessIds);

public interface IGridComponentStateProbe
{
    Task<GridComponentState> CaptureAsync(
        CancellationToken cancellationToken = default);
}

public sealed class WindowsGridComponentStateProbe : IGridComponentStateProbe
{
    public Task<GridComponentState> CaptureAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
            return Task.FromResult(new GridComponentState(false, "", "", new HashSet<int>()));

        using var service = Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Services\NATService");
        var processes = Process.GetProcessesByName("natsvc");
        try
        {
            return Task.FromResult(new GridComponentState(
                service is not null,
                service?.GetValue("ImagePath")?.ToString() ?? "",
                service?.GetValue("Start")?.ToString() ?? "",
                processes.Select(item => item.Id).ToHashSet()));
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}

public sealed class GridComponentEventSource(
    IGridComponentStateProbe probe,
    TimeSpan interval)
{
    public async Task RunAsync(
        Func<MonitoringEvent, CancellationToken, Task> publish,
        CancellationToken cancellationToken)
    {
        GridComponentState? previous = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var current = await probe.CaptureAsync(cancellationToken);
            foreach (var item in DetectChanges(previous, current))
                await publish(item, cancellationToken);
            previous = current;
            await Task.Delay(interval, cancellationToken);
        }
    }

    public static IReadOnlyList<MonitoringEvent> DetectChanges(
        GridComponentState? previous,
        GridComponentState current)
    {
        var now = DateTimeOffset.UtcNow;
        var events = new List<MonitoringEvent>();
        if (current.ServicePresent && previous?.ServicePresent != true)
            events.Add(new("service-created", "NATService", now,
                ServiceProperties(current)));
        else if (current.ServicePresent && previous is not null &&
                 (previous.ServiceImagePath != current.ServiceImagePath ||
                  previous.ServiceStartType != current.ServiceStartType ||
                  (previous.ProcessIds.Count > 0) != (current.ProcessIds.Count > 0)))
            events.Add(new("service-state-changed", "NATService", now,
                ServiceProperties(current)));

        var previousProcesses = previous?.ProcessIds ?? new HashSet<int>();
        foreach (var processId in current.ProcessIds.Except(previousProcesses))
            events.Add(new("process-created", processId.ToString(), now,
                new Dictionary<string, string>
                {
                    ["processName"] = "natsvc",
                    ["serviceName"] = "NATService"
                }));
        return events;
    }

    private static IReadOnlyDictionary<string, string> ServiceProperties(
        GridComponentState state) => new Dictionary<string, string>
        {
            ["serviceName"] = "NATService",
            ["serviceImagePath"] = state.ServiceImagePath,
            ["serviceStartType"] = state.ServiceStartType,
            ["processState"] = state.ProcessIds.Count > 0 ? "running" : "stopped"
        };
}
