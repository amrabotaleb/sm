using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;

namespace QIE.SM.Infrastructure.Repositories;

/// <summary>
/// Reads enrollment manifests from MongoDB.
/// </summary>
public sealed class MongoEnrollmentManifestRepository : IEnrollmentManifestRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<MongoEnrollmentManifestRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoEnrollmentManifestRepository"/> class.
    /// </summary>
    /// <param name="options">The Mongo options.</param>
    /// <param name="logger">The logger.</param>
    public MongoEnrollmentManifestRepository(IOptions<MongoOptions> options, ILogger<MongoEnrollmentManifestRepository> logger)
    {
        _logger = logger;
        var client = new MongoClient(options.Value.ConnectionString);
        var database = client.GetDatabase(options.Value.Database);
        _collection = database.GetCollection<BsonDocument>(options.Value.ManifestsCollection);
    }

    /// <inheritdoc />
    public async Task<string?> GetManifestJsonAsync(string manifestId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("manifestId", manifestId);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            _logger.LogWarning("Manifest {ManifestId} was not found in MongoDB", manifestId);
            return null;
        }

        return document.ToJson();
    }
}
