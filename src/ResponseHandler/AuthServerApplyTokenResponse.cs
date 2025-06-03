using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;
using SharedLibrary.Response.Identity;

namespace AuthService.ResponseHandler;

public class AuthServerApplyTokenResponse
           : IOpenIddictServerHandler<OpenIddictServerEvents.ApplyTokenResponseContext>
{
    private readonly OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> _tokenManager;

    public AuthServerApplyTokenResponse(OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager)
    {
        _tokenManager = tokenManager;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.ApplyTokenResponseContext context)
    {
        if (context.Response.AccessToken is not null)
        {
            if (await _tokenManager.FindByReferenceIdAsync(context.Response.AccessToken).ConfigureAwait(false) is not
                {
                    ExpirationDate: { }
                } authServerToken) return;

            context.Response.RemoveParameter("expires_in");
            context.Response.RemoveParameter("token_type");
            context.Response.AddParameter("payload_token", new OpenIddictParameter(authServerToken.Payload));
        }
    }
}