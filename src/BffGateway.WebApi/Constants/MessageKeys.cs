namespace BffGateway.WebApi.Constants;

/// <summary>
/// Constants for localization message keys
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
        public const string PasswordPolicy = "PasswordPolicy";
        public const string AmountGreaterThanZero = "AmountGreaterThanZero";
        public const string AmountMaxValue = "AmountMaxValue";
        public const string CurrencyRequired = "CurrencyRequired";
        public const string CurrencyInvalid = "CurrencyInvalid";
        public const string DestinationAccountRequired = "DestinationAccountRequired";
        public const string DestinationAccountMaxLength = "DestinationAccountMaxLength";
        public const string ValidationErrorsTitle = "ValidationErrorsTitle";
        public const string ValidationErrorsMessage = "ValidationErrorsMessage";
    }

    /// <summary>
    /// Error message keys
    /// </summary>
    public static class Errors
    {
        public const string ServiceUnavailable = "ServiceUnavailable";
        public const string ServiceUnavailableDetail = "ServiceUnavailableDetail";
        public const string GatewayTimeout = "GatewayTimeout";
        public const string GatewayTimeoutDetail = "GatewayTimeoutDetail";
        public const string BadGateway = "BadGateway";
        public const string BadGatewayDetail = "BadGatewayDetail";
        public const string BadRequest = "BadRequest";
        public const string BadRequestDetail = "BadRequestDetail";
        public const string Unauthorized = "Unauthorized";
        public const string UnauthorizedDetail = "UnauthorizedDetail";
        public const string InternalServerError = "InternalServerError";
        public const string InternalServerErrorDetail = "InternalServerErrorDetail";
    }
}
