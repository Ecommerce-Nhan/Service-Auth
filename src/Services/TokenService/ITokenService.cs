using OpenIddict.Abstractions;
using System.Security.Claims;

namespace AuthService.Services.TokenService;

public interface ITokenService
{
    Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(OpenIddictRequest request);
    Task<string> GetPayload(string token);
}