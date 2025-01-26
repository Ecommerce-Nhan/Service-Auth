using Serilog.Debugging;
using Serilog;

namespace IdentityService.Extentions;

internal static class HostingExtensions
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
        SelfLog.Enable(msg => Log.Information(msg));
        Log.Information("Starting server.");
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog();

        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHttpClient();

        builder.Services.AddOptionPattern();
        builder.Services.AddCustomOpenIddict();
        builder.Services.AddCustomDbContext(builder.Configuration);
        builder.Services.AddGrpcConfiguration(builder.Configuration);

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app, WebApplicationBuilder builder)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseSerilogRequestLogging();
        app.UseDeveloperExceptionPage();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.UseCors();
        app.MapControllers();

        return app;
    }
}