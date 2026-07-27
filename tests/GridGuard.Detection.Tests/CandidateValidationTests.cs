using System.Security.Cryptography;
using GridGuard.Detection;
using GridGuard.Monitoring;

namespace GridGuard.Detection.Tests;

public sealed class CandidateValidationTests
{
    [Fact]
    public void NormalizesCaseQuotesWhitespaceExtensionsAndRegistryHives()
    {
        var result = CandidateNormalizer.Normalize(Catalog(
            new CandidateCatalogRow("  \"GridMember\"  ", " gridmember ", null, null)));
        Assert.Contains(result.Candidates, item =>
            item.Type == "executableName" && item.Value == "GridMember.exe");
        Assert.Contains(result.Candidates, item =>
            item.Type == "serviceName" && item.Value == "gridmember");
        Assert.Equal(
            @"HKEY_LOCAL_MACHINE\Software\Vendor",
            CandidateNormalizer.NormalizeRegistryPath(@" HKLM/Software/Vendor "));
    }

    [Fact]
    public void RemovesDuplicatedServiceNamesCaseInsensitively()
    {
        var result = CandidateNormalizer.Normalize(Catalog(
            new("a.exe", "DuplicateSvc", null, null),
            new("b.exe", "duplicatesvc", null, null)));
        Assert.Equal(1, result.DuplicatesRemoved);
        Assert.Single(result.Candidates, item => item.Type == "serviceName");
    }

    [Fact]
    public void KeepsFilenameCollisionSeparateFromVendorServices()
    {
        var result = CandidateNormalizer.Normalize(Catalog(
            new("shared.exe", "VendorOneSvc", null, null),
            new("SHARED.EXE", "VendorTwoSvc", null, null)));
        Assert.Single(result.Candidates, item => item.Type == "executableName");
        Assert.Equal(2, result.Candidates.Count(item => item.Type == "serviceName"));
        Assert.DoesNotContain(result.Candidates, item =>
            item.Type == "executableName" &&
            item.Classification == CandidateClassification.PotentialGridComponent);
    }

    [Fact]
    public void SeparatesVendorUpdaterFromPotentialGridComponent()
    {
        var result = CandidateNormalizer.Normalize(Catalog(
            new("VendorUpdate.exe", "Vendor Update Service", null, null),
            new("TGridService.exe", "TGridService", null, null)));
        Assert.Contains(result.Candidates, item =>
            item.Value == "VendorUpdate.exe" &&
            item.Classification == CandidateClassification.VendorApplication);
        Assert.Contains(result.Candidates, item =>
            item.Value == "TGridService.exe" &&
            item.Classification == CandidateClassification.PotentialGridComponent);
    }

    [Fact]
    public async Task CorrelatesServiceNameWithImagePath()
    {
        var normalization = CandidateNormalizer.Normalize(Catalog(
            new CandidateCatalogRow("TGridService.exe", "TGridService", null, null)));
        var snapshot = Snapshot(new InventoryRecord(
            "service",
            "TGridService",
            new Dictionary<string, string>
            {
                ["serviceName"] = "TGridService",
                ["serviceDisplayName"] = "TGridService",
                ["serviceImagePath"] = @"C:\Vendor\TGridService.exe",
                ["serviceStartType"] = "2",
                ["serviceState"] = "Running"
            }));
        var report = await new CandidateAuditService(new FakeInspector())
            .AuditAsync(normalization, snapshot);
        Assert.Equal(2, report.Matches.Count);
        Assert.Equal("StrongCandidate", Assert.Single(report.Correlations).Confidence);
    }

    [Fact]
    public async Task CorrelatesAutorunNameWithExecutable()
    {
        var normalization = CandidateNormalizer.Normalize(Catalog(
            new CandidateCatalogRow("Gridmember.exe", "gridmember", "gridmember.exe", null)));
        var snapshot = Snapshot(new InventoryRecord(
            "autorun",
            "synthetic-run",
            new Dictionary<string, string>
            {
                ["registryPath"] = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run",
                ["entryName"] = "gridmember.exe",
                ["commandLine"] = @"C:\Vendor\Gridmember.exe"
            }));
        var report = await new CandidateAuditService(new FakeInspector())
            .AuditAsync(normalization, snapshot);
        Assert.Contains(report.Matches, item => item.CandidateType == "startupEntry");
        Assert.Contains(report.Matches, item => item.CandidateType == "executableName");
        Assert.Equal("StrongCandidate", Assert.Single(report.Correlations).Confidence);
    }

    [Fact]
    public async Task HashesOnlyMatchedFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gridguard-m16-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "TGridService.exe");
        await File.WriteAllTextAsync(path, "synthetic matched candidate");
        try
        {
            var expected = Convert.ToHexString(
                SHA256.HashData(await File.ReadAllBytesAsync(path))).ToLowerInvariant();
            var metadata = await new MatchedFileInspector().InspectAsync(path);
            Assert.NotNull(metadata);
            Assert.Equal(expected, metadata.Sha256);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void RedactsUserHostAndProfile()
    {
        var redactor = new PrivacyRedactor(
            "alice", "WORKSTATION", @"C:\Users\alice");
        var result = redactor.Redact(
            @"WORKSTATION C:\Users\alice\AppData\Vendor\agent.exe alice");
        Assert.DoesNotContain("alice", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WORKSTATION", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<USER_PROFILE>", result);
    }

    [Fact]
    public void PromotionPolicyNeverAutomaticallyReturnsConfirmed()
    {
        Assert.Equal(
            "RecommendConfirmationReview",
            CandidatePromotionPolicy.Recommend(
                CandidateClassification.PotentialGridComponent,
                independentNonCircularSources: 2,
                hasPlausibleGenericInterpretation: false));
        Assert.Equal(
            "VendorApplication",
            CandidatePromotionPolicy.Recommend(
                CandidateClassification.VendorApplication,
                independentNonCircularSources: 2,
                hasPlausibleGenericInterpretation: false));
    }

    [Fact]
    public void PromotionPolicyCountsIndependentPrimaryIdentityEvidenceOnly()
    {
        var sources = new[]
        {
            Source("vendor-a", "vendor-a"),
            Source("mirror-a", "vendor-a"),
            Source("security-b", "security-b"),
            Source("secondary-c", "secondary-c") with { IsPrimary = false },
            Source("circular-d", "circular-d") with { IsCircular = true }
        };

        var result = CandidatePromotionPolicy.Evaluate(
            CandidateClassification.PotentialGridComponent,
            sources,
            hasPlausibleGenericInterpretation: false);

        Assert.Equal(2, result.QualifyingSourceCount);
        Assert.Equal("RecommendConfirmationReview", result.Recommendation);
        Assert.NotEqual("Confirmed", result.Recommendation);
    }

    [Fact]
    public void PromotionPolicyFailsClosedForGenericOrUnreproducibleEvidence()
    {
        var result = CandidatePromotionPolicy.Evaluate(
            CandidateClassification.PotentialGridComponent,
            [
                Source("snippet", "search") with { HasReproducibleIdentity = false },
                Source("generic", "vendor")
            ],
            hasPlausibleGenericInterpretation: true);

        Assert.Equal("StrongCandidate", result.Recommendation);
        Assert.Equal(1, result.QualifyingSourceCount);
        Assert.Contains(result.Reasons, reason => reason.Contains("generic"));
    }

    private static CandidateCatalog Catalog(params CandidateCatalogRow[] rows) => new(rows);

    private static CandidatePromotionPolicy.EvidenceSource Source(
        string sourceId,
        string controlId) =>
        new(sourceId, controlId, true, true, true, false);

    private static InventorySnapshot Snapshot(params InventoryRecord[] records) =>
        new(DateTimeOffset.UtcNow, records, []);

    private sealed class FakeInspector : IMatchedFileInspector
    {
        public Task<MatchedFileMetadata?> InspectAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MatchedFileMetadata?>(new(
                path,
                new string('a', 64),
                "not-signed",
                null,
                "Synthetic",
                "1.0"));
    }
}
