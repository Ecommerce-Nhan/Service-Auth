using IdentityService.Extentions;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);
    HostingExtensions.ConfigureSerilog(builder);

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline(builder);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}