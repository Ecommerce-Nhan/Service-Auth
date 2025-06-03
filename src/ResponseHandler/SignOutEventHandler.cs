using OpenIddict.Server;

namespace AuthService.ResponseHandler;

public class SignOutEventHandler : IOpenIddictServerHandler<OpenIddictServerEvents.ProcessSignOutContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ProcessSignOutContext>()
            .UseSingletonHandler<SignOutEventHandler>()
            .SetOrder(100_000)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    public ValueTask HandleAsync(OpenIddictServerEvents.ProcessSignOutContext context)
    {
        // Implement your custom sign-out logic here

        // Examples:
        // - Clear custom session data
        // - Perform audit logging
        // - Notify other services
        // - Clean up user-specific resources

        return ValueTask.CompletedTask;
    }
}