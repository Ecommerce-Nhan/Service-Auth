using AuthService.Commons;
using Microsoft.EntityFrameworkCore;
using Quartz;
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
}