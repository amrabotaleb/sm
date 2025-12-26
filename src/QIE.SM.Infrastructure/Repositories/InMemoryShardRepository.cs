using System.Collections.Concurrent;
using QIE.SM.Application.Abstractions;
using QIE.SM.Domain;
using QIE.SM.SharedKernel;

namespace QIE.SM.Infrastructure.Repositories;

/// <summary>
/// Provides an in-memory shard repository.
/// </summary>
public sealed class InMemoryShardRepository : IShardRepository
{
    private readonly ConcurrentDictionary<string, Shard> _shards = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<Shard?> GetAsync(string shardId, CancellationToken cancellationToken)
    {
        _shards.TryGetValue(shardId, out var shard);
        return Task.FromResult(shard);
    }

    /// <inheritdoc />
    public Task<PagedResult<Shard>> GetPagedAsync(
        string? modality,
        ShardStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _shards.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(modality))
        {
            query = query.Where(shard => string.Equals(shard.Modality, modality, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            query = query.Where(shard => shard.Status == status.Value);
        }

        var totalCount = query.Count();
        var items = query
            .OrderBy(shard => shard.ShardId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Shard>(items, totalCount, page, pageSize));
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<Shard>> GetActiveAsync(string modality, CancellationToken cancellationToken)
    {
        var results = _shards.Values
            .Where(shard => string.Equals(shard.Modality, modality, StringComparison.OrdinalIgnoreCase))
            .Where(shard => shard.Status == ShardStatus.Active)
            .OrderBy(shard => shard.ShardId)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<Shard>>(results);
    }

    /// <inheritdoc />
    public Task UpsertAsync(Shard shard, CancellationToken cancellationToken)
    {
        shard.UpdatedUtc = DateTime.UtcNow;
        _shards.AddOrUpdate(shard.ShardId, shard, (_, _) => shard);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateStatusAsync(string shardId, ShardStatus status, CancellationToken cancellationToken)
    {
        if (_shards.TryGetValue(shardId, out var shard))
        {
            shard.Status = status;
            shard.UpdatedUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<FleetOverview> GetFleetOverviewAsync(CancellationToken cancellationToken)
    {
        var shards = _shards.Values.ToList();
        var overview = new FleetOverview
        {
            Total = shards.Count,
            Active = shards.Count(shard => shard.Status == ShardStatus.Active),
            Stopped = shards.Count(shard => shard.Status == ShardStatus.Stopped),
            Draining = shards.Count(shard => shard.Status == ShardStatus.Draining),
            Failed = shards.Count(shard => shard.Status == ShardStatus.Failed)
        };

        return Task.FromResult(overview);
    }
}
