using GridGuard.Response;
using GridGuard.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
    options.ServiceName = "GridGuard");
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("GridGuard"));
builder.Services.AddHostedService<GridGuardWorker>();
var host = builder.Build();

var options = builder.Configuration.GetSection("GridGuard").Get<ServiceOptions>() ?? new();
var errors = ResponseConfigurationValidator.Validate(options.Response);
errors = errors.Concat(GridAutoRemovalPolicy.Validate(options.AutoRemoval)).ToArray();
if (errors.Count > 0)
{
    foreach (var error in errors) Console.Error.WriteLine(error);
    return 78;
}

await host.RunAsync();
return 0;
