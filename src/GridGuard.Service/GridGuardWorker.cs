using System.IO.Pipes;
using System.Text.Json;
using GridGuard.Monitoring;
using GridGuard.Response;
using Microsoft.Extensions.Options;

namespace GridGuard.Service;

public sealed record ServiceOptions
{
    public ResponseConfiguration Response { get; init; } = new();
    public int ReconciliationSeconds { get; init; } = 300;
}

public sealed class GridGuardWorker(
    ILogger<GridGuardWorker> logger,
    IOptions<ServiceOptions> options) : BackgroundService
{
    private readonly WindowsInventoryAdapter _inventory = new();
    private DateTimeOffset? _lastScan;
    private int _recordCount;
    private volatile bool _paused;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "GridGuard starting in {Mode}; no network listener is enabled.",
            options.Value.Response.Mode);
        var statusTask = ServeStatusAsync(stoppingToken);
        var reconciliation = new ReconciliationLoop(
            _inventory,
            TimeSpan.FromSeconds(Math.Max(10, options.Value.ReconciliationSeconds)),
            (snapshot, _) =>
            {
                _lastScan = snapshot.CapturedAt;
                _recordCount = snapshot.Records.Count;
                logger.LogInformation(
                    "Audit reconciliation captured {Count} records and {Errors} errors.",
                    snapshot.Records.Count, snapshot.Errors.Count);
                return Task.CompletedTask;
            });
        await Task.WhenAll(statusTask, reconciliation.RunAsync(stoppingToken));
    }

    private async Task ServeStatusAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                "GridGuard.Status.v1", PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.WaitForConnectionAsync(cancellationToken);
            using var reader = new StreamReader(pipe, leaveOpen: true);
            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            var command = (await reader.ReadLineAsync(cancellationToken) ?? "status").Trim();
            if (command.Equals("pause", StringComparison.OrdinalIgnoreCase)) _paused = true;
            if (command.Equals("resume", StringComparison.OrdinalIgnoreCase)) _paused = false;
            if (command.Equals("scan", StringComparison.OrdinalIgnoreCase) && !_paused)
            {
                var snapshot = await _inventory.CaptureAsync(cancellationToken);
                _lastScan = snapshot.CapturedAt;
                _recordCount = snapshot.Records.Count;
            }
            await writer.WriteLineAsync(JsonSerializer.Serialize(new
            {
                serviceState = "running",
                monitoringState = _paused ? "paused" : "active",
                mode = options.Value.Response.Mode.ToString(),
                lastScan = _lastScan,
                inventoryCount = _recordCount,
                permanentDeletion = false
            }));
        }
    }
}
