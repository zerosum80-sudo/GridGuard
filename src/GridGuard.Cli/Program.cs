using GridGuard.Cli;
using GridGuard.Monitoring;

return await CliApplication.RunAsync(
    args,
    Console.Out,
    Console.Error,
    new WindowsInventoryAdapter(),
    Directory.GetCurrentDirectory());

