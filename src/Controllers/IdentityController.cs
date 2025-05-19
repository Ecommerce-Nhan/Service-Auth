using AuthService.Services.TokenService;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static SharedLibrary.Constants.Identity.AuthConstants;

namespace IdentityService.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class IdentityController : ControllerBase
{
    private readonly ITokenService _tokenService;
    public IdentityController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Token()
    {
        try
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                         ?? throw new InvalidOperationException(ErrorConstants.OpenIDRequest);

            return request.GrantType switch
            {
                GrantTypes.AuthorizationCode or GrantTypes.RefreshToken => await HandleAuthorizationOrRefreshTokenAsync(),
                GrantTypes.Password => await HandlePasswordGrantAsync(request),
                _ => CreateBadRequestResponse(Errors.UnsupportedGrantType, ErrorConstants.GrantType)
            };
        }
        catch (Exception ex)
        {
            return CreateBadRequestResponse(Errors.ServerError, ex.Message);
        }
    }

    #region Private methods

    private IActionResult CreateBadRequestResponse(string error, string errorDescription)
    {
        return BadRequest(new OpenIddictResponse
        {
            Error = error,
            ErrorDescription = errorDescription
        });
    }

    private async Task<IActionResult> HandleAuthorizationOrRefreshTokenAsync()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (result.Failure is not null)
            return CreateBadRequestResponse(Errors.InvalidRequest, FormatException(result.Failure));

        if (result.Principal is null)
            return CreateBadRequestResponse(Errors.InvalidClient, Errors.AccessDenied);

        return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandlePasswordGrantAsync(OpenIddictRequest request)
    {
        request.Scope = Scopes.OfflineAccess;
        var claimsPrincipal = await _tokenService.CreateClaimsPrincipalAsync(request);
        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static string FormatException(Exception ex) =>
        $"{ex.Message}{(ex.InnerException != null ? ": " + ex.InnerException.Message : string.Empty)}";

    #endregion Private methods
}