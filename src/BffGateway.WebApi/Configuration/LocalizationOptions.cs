namespace BffGateway.WebApi.Configuration;

/// <summary>
/// Configuration options for localization
/// </summary>
public class LocalizationOptions
{
    public const string SectionName = "Localization";

    /// <summary>
    /// List of supported language codes (e.g., "en", "ar")
    /// </summary>
    public string[] SupportedLanguages { get; set; } = { "en" };

    /// <summary>
    /// Default culture code
    /// </summary>
    public string DefaultCulture { get; set; } = "en";

    /// <summary>
    /// Path to the resources folder
    /// </summary>
    public string ResourcesPath { get; set; } = "Resources";
}
