namespace BffGateway.Application.Constants;

/// <summary>
/// Constants for localization message keys in the Application layer
/// </summary>
public static class MessageKeys
{
    /// <summary>
    /// Validation message keys
    /// </summary>
    public static class Validation
    {
        public const string UsernameRequired = "UsernameRequired";
        public const string UsernameMaxLength = "UsernameMaxLength";
        public const string PasswordRequired = "PasswordRequired";
        public const string AmountGreaterThanZero = "AmountGreaterThanZero";
        public const string AmountMaxValue = "AmountMaxValue";
        public const string CurrencyRequired = "CurrencyRequired";
        public const string CurrencyInvalid = "CurrencyInvalid";
        public const string DestinationAccountRequired = "DestinationAccountRequired";
        public const string DestinationAccountMaxLength = "DestinationAccountMaxLength";
    }

    /// <summary>
    /// General error message keys
    /// </summary>
    public static class Errors
    {
        public const string InternalServerError = "InternalServerError";
    }

    /// <summary>
    /// Authentication message keys
    /// </summary>
    public static class Auth
    {
        public const string LoginSuccess = "LoginSuccess";
        public const string LoginFailed = "LoginFailed";
    }

    /// <summary>
    /// Payment message keys
    /// </summary>
    public static class Payments
    {
        public const string PaymentSuccess = "PaymentSuccess";
        public const string PaymentFailed = "PaymentFailed";
    }
}
