using System.ComponentModel.DataAnnotations;

namespace AuthService.Commons;

public class AuthOptions
{
    [Required(AllowEmptyStrings = false)]
    public string? UserServiceEndpoint { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? ServerIssuer { get; set; }
}