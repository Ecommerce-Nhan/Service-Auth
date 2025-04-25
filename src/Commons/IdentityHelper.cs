using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Security.Claims;

namespace AuthService.Helpers;

public static class IdentityHelper
{
    public static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name or
            Claims.Subject
               => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            _ => new[] { Destinations.AccessToken },
        };
    }

    public static string FormatException(Exception ex) =>
        $"{ex.Message}{(ex.InnerException != null ? ": " + ex.InnerException.Message : string.Empty)}";
}
