using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using static IdentityService.Commons.Constants;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityService.Controllers;

public class AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictTokenManager tokenManager,
        IOpenIddictScopeManager scopeManager) : ControllerBase
{
    #region Inject

    private readonly IOpenIddictApplicationManager _applicationManager = applicationManager;
    private readonly IOpenIddictTokenManager _tokenManager = tokenManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager = authorizationManager;

    #endregion Inject

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() 
                      ?? throw new InvalidOperationException(ErrorConstants.OpenIDRequest);

        var claimsPrincipal = await CreateClaimsPrincipalAsync(request);
        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost]
    [Route("connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> ConnectToken()
    {
        try
        {
            var request = HttpContext.GetOpenIddictServerRequest() 
                          ?? throw new InvalidOperationException(ErrorConstants.OpenIDRequest);

            var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (authenticateResult.Failure is not null)
            {
                var failureMessage = authenticateResult.Failure.Message;
                var failureException = authenticateResult.Failure.InnerException;
                return BadRequest(new OpenIddictResponse
                {
                    Error = Errors.InvalidRequest,
                    ErrorDescription = failureMessage + failureException
                });
            }
            else if (authenticateResult.Principal == null)
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = Errors.InvalidClient,
                    ErrorDescription = Errors.AccessDenied
                });
            }

            var claimsPrincipal = authenticateResult.Principal;
            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            return CreateBadRequestResponse(Errors.UnsupportedGrantType, ErrorConstants.GrantType);
        }
        catch (Exception ex)
        {
            return CreateBadRequestResponse(Errors.ServerError, ex.Message);
        }
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> LogOut()
    {
        await HttpContext.SignOutAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return SignOut(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                       properties: new AuthenticationProperties
                       {
                           RedirectUri = "/"
                       });
    }

    #region Private Methos 

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name or
            Claims.Subject
               => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            _  => new[] { Destinations.AccessToken },
        };
    }
    private IActionResult CreateBadRequestResponse(string error, string errorDescription)
    {
        return BadRequest(new
        {
            error,
            errorDescription
        });
    }
    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(OpenIddictRequest request)
    {
        var claimsIdentity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var role = (string?)request.GetParameter("role");
        //claimsIdentity.SetClaim(Claims.Subject, request.Username);
        claimsIdentity.SetClaim(Claims.Subject, "Subject");
        claimsIdentity.SetClaim(Claims.Audience, "https://localhost:5001");
        claimsIdentity.SetClaim(Claims.Name, request.UserCode);
        claimsIdentity.SetClaim(Claims.Role, role);
        claimsIdentity.SetClaim(Claims.JwtId, Guid.NewGuid().ToString());
        claimsIdentity.SetResources(await scopeManager.ListResourcesAsync(claimsIdentity.GetScopes()).ToListAsync());
        claimsIdentity.SetDestinations(GetDestinations);
        if (!request.IsRefreshTokenGrantType())
        {
            request.Scope = Scopes.OfflineAccess;
            claimsIdentity.SetScopes(Scopes.OfflineAccess);
        }
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        claimsPrincipal.SetScopes(request.GetScopes());

        return claimsPrincipal;
    }



    #endregion Private Methods
}
