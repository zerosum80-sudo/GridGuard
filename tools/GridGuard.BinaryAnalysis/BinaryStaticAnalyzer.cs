using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GridGuard.BinaryAnalysis;

public sealed record SectionReport(
    string Name,
    int VirtualSize,
    int RawSize,
    int RawOffset,
    double Entropy);

public sealed record BinaryAnalysisReport(
    string FileName,
    long Size,
    string Sha256,
    string Sha1,
    string Md5,
    string Machine,
    bool IsPe,
    bool IsPe32Plus,
    IReadOnlyList<SectionReport> Sections,
    bool HasResourceDirectory,
    bool HasImportDirectory,
    string AuthenticodeStatus,
    string? AuthenticodeSubject,
    IReadOnlyList<string> AutoItIndicators,
    string Confidence,
    DateTimeOffset AnalyzedAt);

public static class BinaryStaticAnalyzer
{
    private static readonly byte[][] AutoItMarkers =
    [
        "AU3!EA05"u8.ToArray(),
        "AU3!EA06"u8.ToArray(),
        "AU3!JB01"u8.ToArray()
    ];

    public static async Task<BinaryAnalysisReport> AnalyzeAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Static input was not found: {path}", path);
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        await using var stream = new MemoryStream(bytes, writable: false);
        using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
        if (!pe.HasMetadata && pe.PEHeaders.PEHeader is null)
        {
            throw new BadImageFormatException("Input does not contain a valid PE header.");
        }

        var headers = pe.PEHeaders;
        var sections = headers.SectionHeaders.Select(section =>
        {
            var start = Math.Max(0, section.PointerToRawData);
            var count = Math.Min(section.SizeOfRawData, Math.Max(0, bytes.Length - start));
            var entropy = count == 0 ? 0 : CalculateEntropy(bytes.AsSpan(start, count));
            return new SectionReport(
                section.Name,
                section.VirtualSize,
                section.SizeOfRawData,
                section.PointerToRawData,
                Math.Round(entropy, 4));
        }).ToArray();

        var indicators = AutoItMarkers
            .Where(marker => bytes.AsSpan().IndexOf(marker) >= 0)
            .Select(marker => System.Text.Encoding.ASCII.GetString(marker))
            .ToArray();

        var (signatureStatus, signatureSubject) = ReadAuthenticode(path);
        var peHeader = headers.PEHeader!;
        return new BinaryAnalysisReport(
            Path.GetFileName(path),
            bytes.LongLength,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant(),
            Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant(),
            headers.CoffHeader.Machine.ToString(),
            true,
            peHeader.Magic == PEMagic.PE32Plus,
            sections,
            peHeader.ResourceTableDirectory.Size > 0,
            peHeader.ImportTableDirectory.Size > 0,
            signatureStatus,
            signatureSubject,
            indicators,
            indicators.Length > 0 ? "strong-inference" : "observation-only",
            DateTimeOffset.UtcNow);
    }

    internal static double CalculateEntropy(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return 0;
        Span<int> counts = stackalloc int[256];
        foreach (var value in data) counts[value]++;
        var entropy = 0d;
        foreach (var count in counts)
        {
            if (count == 0) continue;
            var probability = (double)count / data.Length;
            entropy -= probability * Math.Log2(probability);
        }
        return entropy;
    }

    private static (string Status, string? Subject) ReadAuthenticode(string path)
    {
        try
        {
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
            return ("signature-present-unverified-chain", certificate.Subject);
        }
        catch (CryptographicException)
        {
            return ("not-signed", null);
        }
    }
}

