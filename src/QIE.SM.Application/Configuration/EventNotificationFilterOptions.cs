namespace QIE.SM.Application.Configuration;

/// <summary>
/// Represents notification filtering options.
/// </summary>
public sealed class EventNotificationFilterOptions
{
    /// <summary>
    /// Gets or sets allowed sources. Empty means allow all.
    /// </summary>
    public List<string> AllowedSources { get; set; } = new();

    /// <summary>
    /// Gets or sets allowed event types. Empty means allow all.
    /// </summary>
    public List<string> AllowedEventTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets allowed severity values. Empty means allow all.
    /// </summary>
    public List<string> AllowedSeverities { get; set; } = new();
}
