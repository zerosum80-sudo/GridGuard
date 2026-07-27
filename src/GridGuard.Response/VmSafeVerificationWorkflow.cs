using GridGuard.Core;
using GridGuard.Monitoring;

namespace GridGuard.Response;

public static class VmSafeVerificationWorkflow
{
    public static async Task<IReadOnlyList<VerificationEvidence>> VerifyAsync(
        DetectionResult detection,
        IEnumerable<string> filePaths,
        string disposableMetadataRoot,
        CancellationToken cancellationToken = default)
    {
        var paths = filePaths.ToArray();
        var audit = await new ResponseExecutor(
            new ResponseConfiguration(ResponseMode.AuditOnly),
            new QuarantineStore(disposableMetadataRoot))
            .ExecuteAsync(detection, paths, cancellationToken);
        var simulate = await new ResponseExecutor(
            new ResponseConfiguration(ResponseMode.Simulate, ExplicitlyEnabled: true),
            new QuarantineStore(disposableMetadataRoot))
            .ExecuteAsync(detection, paths, cancellationToken);
        return audit.Select(outcome => new VerificationEvidence(
                "AuditOnly", outcome.Status, outcome.Performed, outcome.Detail))
            .Concat(simulate.Select(outcome => new VerificationEvidence(
                "Simulate", outcome.Status, outcome.Performed, outcome.Detail)))
            .ToArray();
    }
}
