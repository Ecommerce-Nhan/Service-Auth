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
        public const string CreditVision = "credit_vision";
        public const string CreditVision_ClientId = "credit_vision";
        public const string CreditVision_ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207";
        public const string CreditVision_DisplayName = "Credit Vision Web";
    }

}