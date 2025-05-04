using AuthService.Commons;
using Grpc.Core;
using gRPCServer.User.Protos;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static gRPCServer.User.Protos.UserProtoService;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services.TokenService;

public class TokenService : ITokenService
{
    private readonly AuthOptions _authOptions;
    private readonly UserProtoServiceClient _userProtoServiceClient;
    private readonly IOpenIddictTokenManager _tokenManager;
    public TokenService(IOptions<AuthOptions> authOptions,
                        UserProtoServiceClient userProtoServiceClient,
                        IOpenIddictTokenManager tokenManager)
    {
        _authOptions = authOptions.Value;
        _userProtoServiceClient = userProtoServiceClient;
        _tokenManager = tokenManager;
    }

    public async Task<string> GetPayload(string token)
    {
        var tokenEntity = await _tokenManager.FindByReferenceIdAsync(token);
        if (tokenEntity == null)
            return "Token not found.";
        return await _tokenManager.GetPayloadAsync(tokenEntity) ?? string.Empty;
    }

    public async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(OpenIddictRequest request)
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
                          .SetClaim(Claims.Name, request.Username);

            claimsIdentity.AddClaim(ClaimTypes.Role, loginResponse.UserRole);
            claimsIdentity.AddClaim("Permission", loginResponse.UserPermission);

            if (!request.IsRefreshTokenGrantType())
            {
                claimsIdentity.SetScopes(Scopes.OfflineAccess);
            }
            claimsIdentity.SetDestinations(GetDestinations);

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            throw new Exception(ex.Message);
        }
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name or
            Claims.Subject
               => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            _ => new[] { Destinations.AccessToken },
        };
    }
}