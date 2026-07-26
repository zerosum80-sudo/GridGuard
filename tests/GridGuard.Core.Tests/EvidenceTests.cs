using GridGuard.Core;

namespace GridGuard.Core.Tests;

public sealed class EvidenceTests
{
    [Fact]
    public void ExpandsEnvironmentAndNormalizesSeparators()
    {
        var value = EvidenceNormalizer.Normalize(
            new("executablePath", "%WINDIR%/System32/test.exe", "x")).Value;
        Assert.Contains(@"\System32\test.exe", value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizesHashCaseAndSpacing()
    {
        Assert.Equal(
            "aabb",
            EvidenceNormalizer.Normalize(new("sha256", "AA BB", "x")).Value);
    }
}

