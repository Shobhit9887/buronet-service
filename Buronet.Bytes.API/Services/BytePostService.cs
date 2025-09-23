using Buronet.Bytes.API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Buronet.Bytes.API.Services;

public class BytePostService
{
    private readonly IMongoCollection<BytePost> _postsCollection;

    public BytePostService(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _postsCollection = mongoDatabase.GetCollection<BytePost>(mongoDbSettings.Value.CollectionName);
    }

    public async Task<List<BytePost>> GetAsync() =>
        await _postsCollection.Find(_ => true).ToListAsync();

    public async Task CreateAsync(BytePost newPost) =>
        await _postsCollection.InsertOneAsync(newPost);
}

// Helper class to map appsettings.json to a C# object
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
}
