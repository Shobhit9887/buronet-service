using Buronet.Bytes.API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Buronet.Bytes.API.Services;

public class BytePostService
{
    private readonly IMongoCollection<BytePost> _postsCollection;
    private readonly IMongoCollection<Models.Comment> _commentsCollection;

    public BytePostService(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _postsCollection = mongoDatabase.GetCollection<BytePost>(mongoDbSettings.Value.CollectionName);
        _commentsCollection = mongoDatabase.GetCollection<Models.Comment>("comments");
    }

    public async Task<List<BytePost>> GetAsync() =>
        await _postsCollection.Find(_ => true).ToListAsync();

    public async Task CreateAsync(BytePost newPost) =>
        await _postsCollection.InsertOneAsync(newPost);

    public async Task<List<Models.BytePost>> GetForYouFeedAsync(string userId) =>
            await _postsCollection.Find(_ => true).SortByDescending(p => p.CreatedAt).Limit(10).ToListAsync();

    public async Task<List<Models.BytePost>> GetConnectionsFeedAsync(List<string> connectionIds) =>
        await _postsCollection.Find(p => p.Likes.Any(l => connectionIds.Contains(l)))
                                .SortByDescending(p => p.CreatedAt).Limit(10).ToListAsync();

    public async Task<List<Models.BytePost>> GetPopularFeedAsync() =>
        await _postsCollection.Find(_ => true).SortByDescending(p => p.Likes.Count).Limit(10).ToListAsync();

    //public async Task CreateAsync(Models.BytePost newPost) =>
    //    await _postsCollection.InsertOneAsync(newPost);

    public async Task ToggleLikeAsync(string byteId, string userId)
    {
        var filter = Builders<Models.BytePost>.Filter.Eq(p => p.Id, byteId);
        var bytePost = await _postsCollection.Find(filter).FirstOrDefaultAsync();

        if (bytePost == null) return;

        var update = bytePost.Likes.Contains(userId)
            ? Builders<Models.BytePost>.Update.Pull(p => p.Likes, userId)
            : Builders<Models.BytePost>.Update.PushEach(p => p.Likes, new[] { userId }, position: 0);

        await _postsCollection.UpdateOneAsync(filter, update);
    }

    public async Task<List<Models.Comment>> GetCommentsAsync(string byteId) =>
        await _commentsCollection.Find(c => c.ByteId == byteId).SortByDescending(c => c.CreatedAt).ToListAsync();

    public async Task AddCommentAsync(Models.Comment newComment)
    {
        await _commentsCollection.InsertOneAsync(newComment);

        var filter = Builders<Models.BytePost>.Filter.Eq(p => p.Id, newComment.ByteId);
        var update = Builders<Models.BytePost>.Update.Inc(p => p.CommentCount, 1);
        await _postsCollection.UpdateOneAsync(filter, update);
    }
}

// Helper class to map appsettings.json to a C# object
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
}
