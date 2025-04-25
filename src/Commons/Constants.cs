namespace IdentityService.Commons;

public class Constants
{
    public static class ErrorConstants
    {
        public const string OpenIDRequest = "The OpenID Connect request cannot be retrieved.";
        public const string GrantType = "The specified grant type is not supported.";
        public const string Account = "The mandatory 'username' and/or 'password' parameters are missing.";
    }

    public static class ClientConstants
    {
        public const string UserService = nameof(UserService);
        public const string UserService_ClientId = "user_service";
        public const string UserService_ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207";
        public const string UserService_DisplayName = "User Service API";
    }
}