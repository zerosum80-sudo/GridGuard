using System.Text.Json;
using GridGuard.BinaryAnalysis;

if (args.Length is not 1)
{
    Console.Error.WriteLine("Usage: gridguard-binary-analysis <path-to-file>");
    return 64;
}

try
{
    var report = await BinaryStaticAnalyzer.AnalyzeAsync(args[0]);
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 66;
}
catch (BadImageFormatException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 65;
}

