using System.IO.Pipes;
using System.Text.Json;
using GridGuard.Core;
using GridGuard.Detection;
using GridGuard.Monitoring;
using GridGuard.Response;
using GridGuard.Rules;
using Microsoft.Extensions.Options;

namespace GridGuard.Service;

public sealed record ServiceOptions
{
    public ResponseConfiguration Response { get; init; } = new();
    public int ReconciliationSeconds { get; init; } = 300;
    public GridAutoRemovalOptions AutoRemoval { get; init; } = new();
}

public sealed class GridGuardWorker(
    ILogger<GridGuardWorker> logger,
    IOptions<ServiceOptions> options) : BackgroundService
{
    private readonly WindowsInventoryAdapter _inventory = new();
    private DateTimeOffset? _lastScan;
    private int _recordCount;
    private volatile bool _paused;
    private string _lastRemovalStatus = "not-run";
    private GridAutoRemovalWorkflow? _autoRemoval;
    private GridRule? _autoRemovalRule;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "GridGuard starting in {Mode}; no network listener is enabled.",
            options.Value.Response.Mode);
        ConfigureAutoRemoval();
        var statusTask = ServeStatusAsync(stoppingToken);
        var eventTask = new GridComponentEventSource(
            new WindowsGridComponentStateProbe(),
            TimeSpan.FromSeconds(options.Value.AutoRemoval.MonitorSeconds))
            .RunAsync(
                async (item, token) =>
                {
                    logger.LogInformation(
                        "Grid component event {Kind} observed for {ObjectId}.",
                        item.Kind,
                        item.ObjectId);
                    await EvaluateAutoRemovalAsync(token);
                },
                stoppingToken);
        var reconciliation = new ReconciliationLoop(
            _inventory,
            TimeSpan.FromSeconds(Math.Max(10, options.Value.ReconciliationSeconds)),
            async (snapshot, token) =>
            {
                _lastScan = snapshot.CapturedAt;
                _recordCount = snapshot.Records.Count;
                logger.LogInformation(
                    "Audit reconciliation captured {Count} records and {Errors} errors.",
                    snapshot.Records.Count, snapshot.Errors.Count);
                await EvaluateAutoRemovalAsync(snapshot, token);
            });
        await Task.WhenAll(
            statusTask,
            eventTask,
            reconciliation.RunAsync(stoppingToken));
    }

    private void ConfigureAutoRemoval()
    {
        var configuration = options.Value.AutoRemoval;
        var errors = GridAutoRemovalPolicy.Validate(configuration);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));
        if (!configuration.Enabled)
        {
            logger.LogInformation("Exact NATService automatic removal is disabled.");
            return;
        }
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "Exact NATService automatic removal requires Windows.");
        var rulePath = ResolveRulePath(configuration.RulePath);
        _autoRemovalRule = RuleLoader.LoadFile(rulePath);
        if (!_autoRemovalRule.Id.Equals(
                GridAutoRemovalPolicy.RuleId, StringComparison.Ordinal))
            throw new InvalidOperationException("Configured automatic-removal rule is invalid.");
        _autoRemoval = new GridAutoRemovalWorkflow(
            configuration,
            new WindowsGridComponentHost(configuration),
            new InventoryRuleVerifier(_inventory, _autoRemovalRule),
            new JsonLineGridRemovalAuditSink(configuration.LogPath));
        logger.LogInformation(
            "Exact automatic removal enabled only for {RuleId}.",
            GridAutoRemovalPolicy.RuleId);
    }

    private async Task EvaluateAutoRemovalAsync(CancellationToken cancellationToken)
    {
        if (_autoRemoval is null || _autoRemovalRule is null) return;
        await EvaluateAutoRemovalAsync(
            await _inventory.CaptureAsync(cancellationToken),
            cancellationToken);
    }

    private async Task EvaluateAutoRemovalAsync(
        InventorySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (_autoRemoval is null || _autoRemovalRule is null) return;
        var detection = Evaluate(_autoRemovalRule, snapshot);
        if (detection.Decision is not (
                DetectionDecision.Suspicious or DetectionDecision.Confirmed))
            return;

        logger.LogWarning(
            "Detection {RuleId} matched at {DetectionTime}; exact removal starting.",
            detection.RuleId,
            detection.Timestamp);
        var result = await _autoRemoval.ExecuteAsync(detection, cancellationToken);
        _lastRemovalStatus = result.Status;
        if (result.Errors.Count == 0)
            logger.LogWarning(
                "Removal {Status}; service {Service}; files {Files}; verification {Verification}.",
                result.Status,
                result.RemovedService,
                string.Join(",", result.RemovedFiles),
                result.VerificationResult);
        else
            logger.LogError(
                "Removal {Status}; verification {Verification}; errors {Errors}.",
                result.Status,
                result.VerificationResult,
                string.Join(" | ", result.Errors));
    }

    private static DetectionResult Evaluate(
        GridRule rule,
        InventorySnapshot snapshot) => new DetectionEngine().Evaluate(
            rule,
            snapshot.Records.SelectMany(record =>
                record.Properties.Select(pair =>
                    new EvidenceItem(pair.Key, pair.Value, record.Id))));

    private static string ResolveRulePath(string configured)
    {
        var candidates = new[]
        {
            Path.GetFullPath(configured, Directory.GetCurrentDirectory()),
            Path.GetFullPath(configured, AppContext.BaseDirectory),
            Path.GetFullPath(Path.Combine("..", configured), AppContext.BaseDirectory)
        };
        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "Exact automatic-removal rule file was not found.",
                configured);
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
                autoRemovalRule = GridAutoRemovalPolicy.RuleId,
                autoRemovalEnabled = options.Value.AutoRemoval.Enabled,
                autoRemovalMonitoring = options.Value.AutoRemoval.Enabled ?
                    "active" : "disabled",
                lastRemovalStatus = _lastRemovalStatus,
                permanentDeletion = false
            }));
        }
    }

    private sealed class InventoryRuleVerifier(
        IInventoryAdapter inventory,
        GridRule rule) : IGridRemovalVerifier
    {
        public async Task<bool> IsNoMatchAsync(CancellationToken cancellationToken)
        {
            var snapshot = await inventory.CaptureAsync(cancellationToken);
            return Evaluate(rule, snapshot).Decision == DetectionDecision.Clean;
        }
    }
}
