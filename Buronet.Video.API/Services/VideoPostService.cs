using Buronet.Video.API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Buronet.Video.API.Services;

public class VideoPostService
{
    private readonly IMongoCollection<VideoPost> _postsCollection;

    public VideoPostService(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _postsCollection = mongoDatabase.GetCollection<VideoPost>(mongoDbSettings.Value.CollectionName);
    }

    public async Task<List<VideoPost>> GetAsync() =>
        await _postsCollection.Find(_ => true).ToListAsync();

    public async Task CreateAsync(VideoPost newPost) =>
        await _postsCollection.InsertOneAsync(newPost);
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
}
