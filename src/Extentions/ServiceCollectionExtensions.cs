using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using System.Text;

namespace IdentityService.Extentions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomDbContext(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var connectString = builder.Configuration.GetConnectionString("AppDbConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectString, opt => {
                opt.EnableRetryOnFailure();
            });
            options.UseOpenIddict();
        });

        services.AddQuartz(options =>
        {
            options.UseMicrosoftDependencyInjectionJobFactory();
            options.UseSimpleTypeLoader();
            options.UseInMemoryStore();
        });

        return services;
    }

    public static IServiceCollection AddCustomOpenIddict(this IServiceCollection services)
    {
        services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore()
                   .UseDbContext<ApplicationDbContext>();

            options.UseQuartz()
                   .SetMinimumAuthorizationLifespan(TimeSpan.FromDays(7))
                   .SetMinimumTokenLifespan(TimeSpan.FromDays(1))
                   .SetMaximumRefireCount(3);
        })
        .AddServer(options =>
        {
            options.SetTokenEndpointUris("connect/token")
                   .SetAuthorizationEndpointUris("connect/authorize")
                   .SetLogoutEndpointUris("connect/logout");

            options.AllowAuthorizationCodeFlow()
                   .AllowRefreshTokenFlow()
                   .UseReferenceRefreshTokens();

            options.SetAccessTokenLifetime(TimeSpan.FromDays(1))
                   .SetRefreshTokenLifetime(TimeSpan.FromDays(7));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("IxrAjDoa2FqElO7IhrSrUJELhUckePEPVpaePlS_Xaw"));
            var signingCert = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            options.AddSigningCredentials(signingCert);

            options.AddEphemeralEncryptionKey()
                   .AddEphemeralSigningKey()
                   .DisableAccessTokenEncryption();

            options.UseAspNetCore()
                   .EnableLogoutEndpointPassthrough()
                   .EnableTokenEndpointPassthrough()
                   .EnableAuthorizationEndpointPassthrough();

        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });

        services.AddHostedService<Worker>();

        return services;
    }
}
