using AuthService.ResponseHandler;
using IdentityService;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using System.Text;

namespace AuthService.Extentions;

public static class OpenIddictExtensions
{
    public static IServiceCollection AddCustomOpenIddict(this IServiceCollection services)
    {
        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AuthDbContext>();

                options.UseQuartz()
                       .SetMinimumAuthorizationLifespan(TimeSpan.FromDays(7))
                       .SetMinimumTokenLifespan(TimeSpan.FromDays(1))
                       .SetMaximumRefireCount(3);
            })
            .AddServer(options =>
            {
                options.AcceptAnonymousClients();
                options.TokenConfiguration();
                options.SignatureConfiguration();

                options.AddEventHandler<OpenIddictServerEvents.ApplyTokenResponseContext>(builder =>
                {
                    builder.UseScopedHandler<AuthServerApplyTokenResponse>()
                    .SetType(OpenIddictServerHandlerType.Custom)
                    .Build();
                });
                options.AddEventHandler(SignOutEventHandler.Descriptor);
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });


        return services;
    }

    private static OpenIddictServerBuilder TokenConfiguration(this OpenIddictServerBuilder builder)
    {
        builder.SetTokenEndpointUris("api/identity/token");

        builder.AllowRefreshTokenFlow()
               .AllowPasswordFlow();

        builder.UseReferenceRefreshTokens()
               .UseReferenceAccessTokens();

        builder.SetAccessTokenLifetime(TimeSpan.FromHours(1))
               .SetRefreshTokenLifetime(TimeSpan.FromDays(7));

        return builder;
    }

    private static OpenIddictServerBuilder SignatureConfiguration(this OpenIddictServerBuilder builder)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("IxrAjDoa2FqElO7IhrSrUJELhUckePEPVpaePlS_Xaw"));
        var signingCert = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        builder.AddSigningCredentials(signingCert);

        builder.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey()
               .DisableAccessTokenEncryption();

        builder.UseAspNetCore()
               .EnableLogoutEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough()
               .DisableTransportSecurityRequirement();

        return builder;
    }
}