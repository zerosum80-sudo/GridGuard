using GridGuard.Core;
using GridGuard.Response;
using System.Text.Json;

namespace GridGuard.Response.Tests;

public sealed class GridAutoRemovalTests
{
    [Fact]
    public async Task ExactNatServiceMatchUsesVerifiedRemovalOrder()
    {
        var path = AllowedPath();
        var host = new FakeHost
        {
            Before = new(true, true, true, path)
        };
        var audit = new FakeAudit();
        var result = await new GridAutoRemovalWorkflow(
                Options(path),
                host,
                new FakeVerifier(true),
                audit)
            .ExecuteAsync(Detection(path));

        Assert.Equal("REMOVED", result.Status);
        Assert.Equal(
            ["inspect", "stop:NATService", $"delete-file:{path}",
                "delete-service:NATService", "inspect"],
            host.Calls);
        Assert.Equal("NATService", result.RemovedService);
        Assert.Equal([path], result.RemovedFiles);
        Assert.Equal("NATSERVICE_ABSENT_PROCESS_ABSENT_FILE_ABSENT_RULE_NO_MATCH",
            result.VerificationResult);
        Assert.Same(result, Assert.Single(audit.Records));
    }

    [Fact]
    public async Task OtherRuleNeverMutatesHost()
    {
        var path = AllowedPath();
        var host = new FakeHost();
        var result = await new GridAutoRemovalWorkflow(
                Options(path),
                host,
                new FakeVerifier(true),
                new FakeAudit())
            .ExecuteAsync(Detection(path) with { RuleId = "grid.other.001" });

        Assert.Equal("REFUSED", result.Status);
        Assert.Empty(host.Calls);
    }

    [Fact]
    public async Task FilebogoPathNeverMutatesHost()
    {
        var path = AllowedPath();
        var host = new FakeHost();
        var result = await new GridAutoRemovalWorkflow(
                Options(path),
                host,
                new FakeVerifier(true),
                new FakeAudit())
            .ExecuteAsync(Detection(
                Path.Combine(Path.GetTempPath(), "Filebogo.com",
                    "FilebogoLauncher.exe")));

        Assert.Equal("REFUSED", result.Status);
        Assert.Empty(host.Calls);
        Assert.Contains("exact allowed", result.Errors.Single());
    }

    [Fact]
    public async Task DisabledConfigurationNeverMutatesHost()
    {
        var path = AllowedPath();
        var host = new FakeHost();
        var result = await new GridAutoRemovalWorkflow(
                Options(path) with { Enabled = false },
                host,
                new FakeVerifier(true),
                new FakeAudit())
            .ExecuteAsync(Detection(path));

        Assert.Equal("REFUSED", result.Status);
        Assert.Empty(host.Calls);
    }

    [Fact]
    public async Task VerificationFailureIsLoggedAndFailsClosed()
    {
        var path = AllowedPath();
        var host = new FakeHost
        {
            Presence = new(false, false, true, null)
        };
        var result = await new GridAutoRemovalWorkflow(
                Options(path),
                host,
                new FakeVerifier(false),
                new FakeAudit())
            .ExecuteAsync(Detection(path));

        Assert.Equal("FAILED", result.Status);
        Assert.Equal("VERIFICATION_FAILED", result.VerificationResult);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task RemainingNatsvcProcessFailsVerification()
    {
        var path = AllowedPath();
        var host = new FakeHost
        {
            Presence = new(false, true, false, null)
        };
        var result = await new GridAutoRemovalWorkflow(
                Options(path),
                host,
                new FakeVerifier(true),
                new FakeAudit())
            .ExecuteAsync(Detection(path));

        Assert.Equal("FAILED", result.Status);
        Assert.Equal("VERIFICATION_FAILED", result.VerificationResult);
    }

    [Fact]
    public async Task EvidenceFromDifferentObjectsNeverAuthorizesRemoval()
    {
        var path = AllowedPath();
        var host = new FakeHost();
        var detection = Detection(path) with
        {
            Evidence =
            [
                new("serviceName", "NATService", "service-a"),
                new("serviceImagePath", path, "service-b")
            ]
        };
        var result = await new GridAutoRemovalWorkflow(
                Options(path),
                host,
                new FakeVerifier(true),
                new FakeAudit())
            .ExecuteAsync(detection);

        Assert.Equal("REFUSED", result.Status);
        Assert.Empty(host.Calls);
    }

    [Fact]
    public async Task DirectoryAtExactComponentPathFailsBeforeMutation()
    {
        var path = AllowedPath();
        var host = new FakeHost
        {
            Before = new(true, false, false, path, PathCollisionPresent: true)
        };
        var result = await new GridAutoRemovalWorkflow(
                Options(path),
                host,
                new FakeVerifier(true),
                new FakeAudit())
            .ExecuteAsync(Detection(path));

        Assert.Equal("FAILED", result.Status);
        Assert.Equal("NOT_RUN", result.VerificationResult);
        Assert.Equal(["inspect"], host.Calls);
        Assert.Contains("directory", result.Errors.Single());
    }

    [Fact]
    public void PolicyRejectsBroaderServiceOrPathConfiguration()
    {
        Assert.NotEmpty(GridAutoRemovalPolicy.Validate(
            Options(AllowedPath()) with { ServiceName = "FilebogoLauncher" }));
        Assert.NotEmpty(GridAutoRemovalPolicy.Validate(
            Options(Path.Combine(Path.GetTempPath(), "natsvc.exe"))));
    }

    [Fact]
    public async Task JsonLineAuditContainsRequiredRemovalFields()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), $"gridguard-audit-{Guid.NewGuid():N}");
        var logPath = Path.Combine(directory, "auto-removal.jsonl");
        var path = AllowedPath();
        try
        {
            var host = new FakeHost
            {
                Before = new(true, true, true, path)
            };
            await new GridAutoRemovalWorkflow(
                    Options(path) with { LogPath = logPath },
                    host,
                    new FakeVerifier(true),
                    new JsonLineGridRemovalAuditSink(logPath))
                .ExecuteAsync(Detection(path));

            using var record = JsonDocument.Parse(
                Assert.Single(await File.ReadAllLinesAsync(logPath)));
            var root = record.RootElement;
            Assert.NotEqual(default, root.GetProperty("DetectionTime").GetDateTimeOffset());
            Assert.Equal(GridAutoRemovalPolicy.RuleId,
                root.GetProperty("RuleId").GetString());
            Assert.Equal("NATService", root.GetProperty("RemovedService").GetString());
            Assert.Equal(path,
                root.GetProperty("RemovedFiles").EnumerateArray().Single().GetString());
            Assert.Equal(
                "NATSERVICE_ABSENT_PROCESS_ABSENT_FILE_ABSENT_RULE_NO_MATCH",
                root.GetProperty("VerificationResult").GetString());
            Assert.Empty(root.GetProperty("Errors").EnumerateArray());
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static GridAutoRemovalOptions Options(string path) => new()
    {
        Enabled = true,
        AuthorizedRuleId = GridAutoRemovalPolicy.RuleId,
        ServiceName = GridAutoRemovalPolicy.ServiceName,
        AllowedComponentPath = path,
        LogPath = Path.Combine(Path.GetTempPath(), "gridguard-test-log.jsonl"),
        MonitorSeconds = 1
    };

    private static string AllowedPath() => GridAutoRemovalPolicy.ExpectedComponentPath;

    private static DetectionResult Detection(string path) => new(
        GridAutoRemovalPolicy.RuleId,
        [
            new("serviceName", "NATService", "NATService"),
            new("serviceImagePath", path, "NATService")
        ],
        "strong-inference",
        60,
        DetectionDecision.Suspicious,
        "exact synthetic match",
        DateTimeOffset.UtcNow,
        ["NATService"],
        "exact auto removal");

    private sealed class FakeHost : IGridComponentHost
    {
        public List<string> Calls { get; } = [];
        public GridComponentPresence Before { get; set; } =
            new(false, false, false, null);
        public GridComponentPresence Presence { get; set; } =
            new(false, false, false, null);
        private int _inspections;

        public Task<GridComponentPresence> InspectAsync(
            CancellationToken cancellationToken)
        {
            Calls.Add("inspect");
            return Task.FromResult(_inspections++ == 0 ? Before : Presence);
        }

        public Task StopComponentAsync(
            string serviceName,
            CancellationToken cancellationToken)
        {
            Calls.Add($"stop:{serviceName}");
            return Task.CompletedTask;
        }

        public Task DeleteComponentFileAsync(
            string path,
            CancellationToken cancellationToken)
        {
            Calls.Add($"delete-file:{path}");
            return Task.CompletedTask;
        }

        public Task DeleteServiceAsync(
            string serviceName,
            CancellationToken cancellationToken)
        {
            Calls.Add($"delete-service:{serviceName}");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVerifier(bool noMatch) : IGridRemovalVerifier
    {
        public Task<bool> IsNoMatchAsync(CancellationToken cancellationToken) =>
            Task.FromResult(noMatch);
    }

    private sealed class FakeAudit : IGridRemovalAuditSink
    {
        public List<GridRemovalAuditRecord> Records { get; } = [];

        public Task WriteAsync(
            GridRemovalAuditRecord record,
            CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }
}
