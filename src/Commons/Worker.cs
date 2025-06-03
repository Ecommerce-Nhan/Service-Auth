using IdentityService;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static SharedLibrary.Constants.Identity.AuthConstants;

namespace AuthService.Commons;

public class Worker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public Worker(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync(ClientConstants.UserService_ClientId, cancellationToken) == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ApplicationType = ApplicationTypes.Web,
                ClientId = ClientConstants.UserService_ClientId,
                ClientSecret = ClientConstants.UserService_ClientSecret,
                DisplayName = ClientConstants.UserService_DisplayName,
                RedirectUris = { new Uri("https://localhost:7033") },
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Revocation,

                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,

                    Permissions.Prefixes.Scope + "api.user",
                    Permissions.ResponseTypes.Code,
                    Scopes.OfflineAccess
                },
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
