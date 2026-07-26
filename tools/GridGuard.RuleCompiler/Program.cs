using System.Text.Json;
using GridGuard.RuleCompiler;

if (args is ["--self-test"])
{
    var fixture = new RawIndicator(
        "serviceName",
        "  SyntheticGridService  ",
        "synthetic-fixture",
        "fixture.json#/services/0",
        "unit-test",
        "strong-inference",
        false);
    var normalized = IndicatorNormalizer.Normalize(fixture);
    if (normalized.Value != "SyntheticGridService" ||
        normalized.RuleStatus != "candidate" ||
        normalized.Confidence == "confirmed")
    {
        Console.Error.WriteLine("Indicator normalization self-test failed.");
        return 1;
    }
    Console.WriteLine("Indicator normalization self-test passed.");
    return 0;
}

if (args.Length is not 2)
{
    Console.Error.WriteLine("Usage: gridguard-rule-compiler <raw-indicators.json> <candidate-output.json>");
    return 64;
}

try
{
    var raw = JsonSerializer.Deserialize<List<RawIndicator>>(
        await File.ReadAllTextAsync(args[0]),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidDataException("Indicator input is empty.");
    var output = raw.Select(IndicatorNormalizer.Normalize).ToArray();
    await File.WriteAllTextAsync(
        args[1],
        JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote {output.Length} candidate indicators.");
    return 0;
}
catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or ArgumentException)
{
    Console.Error.WriteLine(ex.Message);
    return 65;
}

