using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;

namespace AuthService.ResponseHandler;

public class AuthServerApplyIntrospectionResponse(OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager)
           : IOpenIddictServerHandler<OpenIddictServerEvents.ApplyIntrospectionResponseContext>
{
    
    public async ValueTask HandleAsync(OpenIddictServerEvents.ApplyIntrospectionResponseContext context)
    {
        if (context.Request?.Token is not null)
        {
            if (await tokenManager
                .FindByReferenceIdAsync(context.Request.Token)
                .ConfigureAwait(false) is not
                {
                    ExpirationDate: { }
                } authServerToken) return;

            if (authServerToken.Type == OpenIddictConstants.TokenTypeHints.AccessToken)
            {
                var expiration = authServerToken.ExpirationDate.Value;
                var now = DateTime.UtcNow;

                if (expiration <= now.AddMinutes(15))
                {
                    authServerToken.ExpirationDate = expiration.AddMinutes(30);
                    await tokenManager.UpdateAsync(authServerToken).ConfigureAwait(false);
                }
            }

            context.Response.AddParameter("payload_token", new OpenIddictParameter(authServerToken.Payload));
        }
    }
}
