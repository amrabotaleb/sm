namespace QIE.SM.Application.Configuration;

/// <summary>
/// Represents shard provisioner configuration.
/// </summary>
public sealed class ShardProvisionerOptions
{
    /// <summary>
    /// Gets or sets the provisioner mode. Supported values: Kubernetes, AgentGrpc.
    /// </summary>
    public string Mode { get; set; } = "Kubernetes";
}
