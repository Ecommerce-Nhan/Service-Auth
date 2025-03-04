using Grpc.Core;
using gRPCServer.User.Protos;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static IdentityService.Commons.Constants;
using static gRPCServer.User.Protos.UserProtoService;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityService.Controllers;

public class AuthorizationController : ControllerBase
{
    private readonly AuthOptions _authOptions;
    private readonly UserProtoServiceClient _userProtoServiceClient;
    private readonly IOpenIddictTokenManager _tokenManager;

    public AuthorizationController(IOptions<AuthOptions> authOptions,
                                   UserProtoServiceClient userProtoServiceClient,
                                   IOpenIddictTokenManager tokenManager)
    {
        _authOptions = authOptions.Value;
        _userProtoServiceClient = userProtoServiceClient;
        _tokenManager = tokenManager;
    }

    [HttpGet("~/auth/authorize")]
    [HttpPost("~/auth/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() 
                      ?? throw new InvalidOperationException(ErrorConstants.OpenIDRequest);

        var claimsPrincipal = await CreateClaimsPrincipalAsync(request);
        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost]
    [Route("auth/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> ConnectToken()
    {
        try
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                          ?? throw new InvalidOperationException(ErrorConstants.OpenIDRequest);

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
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

                return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsPasswordGrantType())
            {
                var claimsPrincipal = await CreateClaimsPrincipalAsync(request);

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
    [HttpPost("~/auth/logout")]
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
        try
        {
            var loginRequest = new LoginRequest
            {
                Username = request.Username,
                Password = request.Password
            };
            var loginResponse = await _userProtoServiceClient.LoginAsync(loginRequest);
            var claimsIdentity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            claimsIdentity.SetClaim(Claims.Subject, loginResponse.UserId)
                          .SetClaim(Claims.Audience, loginResponse.UserId)
                          .SetClaim(Claims.Issuer, _authOptions.ServerIssuer)
                          .SetClaim(Claims.Name, request.Username)
                          .SetClaim(Claims.Role, loginResponse.UserRole);
            if (!request.IsRefreshTokenGrantType())
            {
                claimsIdentity.SetScopes(Scopes.OfflineAccess);
            }
            claimsIdentity.SetClaim(Claims.Subject, request.ClientId);
            claimsIdentity.SetDestinations(GetDestinations);

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        } catch (RpcException ex)
        {
            Console.WriteLine($"Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            throw new Exception(ex.Message);
        }
    }
    private void SetRefreshTokenInCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(10),
            SameSite = SameSiteMode.Strict,
            Secure = true
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    #endregion Private Methods
}
