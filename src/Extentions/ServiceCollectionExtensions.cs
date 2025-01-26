using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using System.Text;
using static gRPCServer.User.Protos.UserProtoService;

namespace IdentityService.Extentions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOptionPattern(this IServiceCollection services)
    {
        services.AddOptions<AuthOptions>().BindConfiguration(nameof(AuthOptions))
                                          .ValidateDataAnnotations()
                                          .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectString = configuration.GetConnectionString("AppDbConnection");
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseNpgsql(connectString, opt =>
            {
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

    public static IServiceCollection AddGrpcConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = configuration.GetSection(nameof(AuthOptions)).Get<AuthOptions>();
        services.AddGrpcClient<UserProtoServiceClient>(s => 
        s.Address = new Uri(authOptions?.UserServiceEndpoint ?? throw new Exception("Missing configure server")));

        return services;
    }

    public static IServiceCollection AddCustomOpenIddict(this IServiceCollection services)
    {
        services.AddOpenIddict()
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
            options.SetTokenEndpointUris("auth/token")
                   .SetAuthorizationEndpointUris("auth/authorize")
                   .SetLogoutEndpointUris("auth/logout");

            options.AllowAuthorizationCodeFlow()
                   .AllowRefreshTokenFlow()
                   .AllowPasswordFlow()
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