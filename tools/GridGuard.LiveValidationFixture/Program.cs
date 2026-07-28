using GridGuard.LiveValidationFixture;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "NATService");
builder.Services.AddHostedService<IdleWorker>();
await builder.Build().RunAsync();

namespace GridGuard.LiveValidationFixture
{
    internal sealed class IdleWorker : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
