namespace QIE.SM.Application.Configuration;

/// <summary>
/// Represents MongoDB configuration options.
/// </summary>
public sealed class MongoOptions
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manifests collection name.
    /// </summary>
    public string ManifestsCollection { get; set; } = string.Empty;
}
