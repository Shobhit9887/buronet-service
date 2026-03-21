using Buronet.Bytes.API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Buronet.Bytes.API.Services;

public class BytePostService
{
    private readonly IMongoCollection<BytePost> _postsCollection;
    private readonly IMongoCollection<Models.Comment> _commentsCollection;
    private readonly BuronetConnectionsClient _connectionsClient;

    public BytePostService(IOptions<MongoDbSettings> mongoDbSettings, BuronetConnectionsClient connectionsClient)
    {
        var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _postsCollection = mongoDatabase.GetCollection<BytePost>(mongoDbSettings.Value.CollectionName);
        _commentsCollection = mongoDatabase.GetCollection<Models.Comment>("comments");
        _connectionsClient = connectionsClient;
    }

    public async Task<List<BytePost>> GetAsync() =>
        await _postsCollection.Find(_ => true).ToListAsync();

    public async Task CreateAsync(BytePost newPost) =>
        await _postsCollection.InsertOneAsync(newPost);

    public async Task<List<Models.BytePost>> GetForYouFeedAsync(string userId, int page = 1, int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var bytes = await _postsCollection.Find(_ => true)
            .SortByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        await EnrichBytesWithProfilePicturesAsync(bytes);
        return bytes;
    }

    public async Task<List<Models.BytePost>> GetConnectionsFeedAsync(List<string> connectionIds, int page = 1, int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var bytes = await _postsCollection.Find(p => p.Likes.Any(l => connectionIds.Contains(l)))
            .SortByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        await EnrichBytesWithProfilePicturesAsync(bytes);
        return bytes;
    }

    public async Task<List<Models.BytePost>> GetPopularFeedAsync(int page = 1, int pageSize = 10)
    {
        // Popular = most likes, ties broken by recency.
        var skip = (page - 1) * pageSize;
        var pipeline = new[]
        {
            new BsonDocument("$addFields",
                new BsonDocument("likesCount",
                    new BsonDocument("$size",
                        new BsonDocument("$ifNull", new BsonArray { "$likes", new BsonArray() })))),
            new BsonDocument("$sort", new BsonDocument { { "likesCount", -1 }, { "createdAt", -1 } }),
            new BsonDocument("$skip", skip),
            new BsonDocument("$limit", pageSize),
            new BsonDocument("$project", new BsonDocument("likesCount", 0))
        };

        var bytes = await _postsCollection.Aggregate<Models.BytePost>(pipeline).ToListAsync();
        await EnrichBytesWithProfilePicturesAsync(bytes);
        return bytes;
    }

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

    public async Task DeleteByteByMediaUrlAsync(string mediaUrl)
    {
        if (string.IsNullOrEmpty(mediaUrl))
            return;

        // Find byte post by media URL
        var filter = Builders<Models.BytePost>.Filter.Or(
            Builders<Models.BytePost>.Filter.Eq(p => p.Submission.MediaUrl, mediaUrl),
            Builders<Models.BytePost>.Filter.Eq(p => p.Submission.Thumbnail, mediaUrl)
        );

        var bytePost = await _postsCollection.Find(filter).FirstOrDefaultAsync();
        if (bytePost == null)
            return;

        // Delete the byte post
        await _postsCollection.DeleteOneAsync(f => f.Id == bytePost.Id);

        // Delete associated comments
        await _commentsCollection.DeleteManyAsync(c => c.ByteId == bytePost.Id);
    }

    private async Task EnrichBytesWithProfilePicturesAsync(List<BytePost> bytes)
    {
        if (bytes == null || bytes.Count == 0)
            return;

        // Extract unique creator IDs
        var creatorIds = bytes
            .Where(b => !string.IsNullOrEmpty(b.Creator?.Id))
            .Select(b => b.Creator.Id)
            .Distinct()
            .ToList();

        if (creatorIds.Count == 0)
            return;

        try
        {
            // Fetch profile pictures in batch
            var profileData = await _connectionsClient.GetBatchUserProfilesAsync(creatorIds);

            // Enrich bytes with profile pictures
            foreach (var bytePost in bytes)
            {
                if (bytePost.Creator != null && profileData.TryGetValue(bytePost.Creator.Id, out var profile))
                {
                    bytePost.Creator.Pic = profile.ProfilePictureUrl;
                    if (string.IsNullOrEmpty(bytePost.Creator.Name))
                    {
                        bytePost.Creator.Name = profile.Name;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception but don't fail the request - return bytes with existing pic data
            System.Diagnostics.Debug.WriteLine($"Error enriching bytes with profile pictures: {ex.Message}");
        }
    }
}

// Helper class to map appsettings.json to a C# object
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
}
