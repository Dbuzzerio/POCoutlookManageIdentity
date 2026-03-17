using PLN.Azure.Function.Common.Attributes;

namespace POCoutlookManageIdentity.OutlookManageTrigger.Options;

/// <summary>
/// Configuration options for the <see cref="OutlookManageTrigger"/> function.
/// </summary>
[ConfigOptions]
[Validate]
[ValidateOnStart]
public class OutlookManageTriggerOptions
{
    // TODO: Add configuration properties

    /// <summary>
    /// Sample configuration property — replace with actual settings.
    /// </summary>
    /// <example>
    /// <code>
    /// "Uri": {
    ///     "OutlookManageTrigger": {
    ///         "Endpoint": "https://example.com/api"
    ///     }
    /// }
    /// </code>
    /// </example>
    //[ConfigSection("Uri")]
    //[ConfigKey("OutlookManageTrigger:Endpoint")]
    //[ConfigDescription("External service endpoint for OutlookManageTrigger")]
    //public string Endpoint { get; set; } = string.Empty;
}
