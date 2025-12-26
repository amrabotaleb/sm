using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using QIE.SM.Application.Abstractions;
using QIE.SM.Domain;

namespace QIE.SM.Infrastructure.Routing;

/// <summary>
/// Routes shards using a consistent hashing strategy.
/// </summary>
public sealed class ConsistentHashShardRouter : IShardRouter
{
    private readonly IShardRepository _shardRepository;
    private readonly ILogger<ConsistentHashShardRouter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsistentHashShardRouter"/> class.
    /// </summary>
    /// <param name="shardRepository">The shard repository.</param>
    /// <param name="logger">The logger.</param>
    public ConsistentHashShardRouter(IShardRepository shardRepository, ILogger<ConsistentHashShardRouter> logger)
    {
        _shardRepository = shardRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ResolveTargetShardAsync(string modality, string identifier, CancellationToken cancellationToken)
    {
        var activeShards = await _shardRepository.GetActiveAsync(modality, cancellationToken);
        if (activeShards.Count == 0)
        {
            throw new InvalidOperationException($"No active shards available for modality {modality}.");
        }

        var ordered = activeShards.OrderBy(shard => shard.ShardId, StringComparer.OrdinalIgnoreCase).ToList();
        var hash = ComputeHash(identifier);
        var index = (int)(hash % (uint)ordered.Count);
        var selected = ordered[index].ShardId;

        _logger.LogInformation("Routed identifier {Identifier} to shard {ShardId} for modality {Modality}", identifier, selected, modality);
        return selected;
    }

    private static uint ComputeHash(string value)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToUInt32(bytes, 0);
    }
}
