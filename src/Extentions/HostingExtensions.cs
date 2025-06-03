using AuthService.Extentions;
using AuthService.Services.TokenService;
using Serilog;

namespace IdentityService.Extentions;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog();

        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen();

        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddOptionPattern();
        builder.Services.AddCustomDbContext(builder.Configuration);
        builder.Services.AddGrpcConfiguration(builder.Configuration);
        builder.Services.AddCustomOpenIddict();
        //builder.Services.AddHostedService<Worker>();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app, WebApplicationBuilder builder)
    {
        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            //app.UseHttpsRedirection();
        }

        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.MapControllers();

        return app;
    }
}