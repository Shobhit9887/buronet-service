using System.Net.Http.Headers;
using System.Text.Json;

namespace Buronet.Bytes.API.Services;

public sealed class BuronetConnectionsClient
{
    private readonly HttpClient _http;

    public BuronetConnectionsClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<string>> GetAllConnectionUserIdsAsync(string bearerToken, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return [];

        var connectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var page = 1; ; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/connections/all?page={page}&pageSize={pageSize}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array)
                break;

            var countThisPage = 0;

            foreach (var item in data.EnumerateArray())
            {
                // buronet-service ConnectionDto has `ConnectedUserId` (Guid)
                if (item.TryGetProperty("ConnectedUserId", out var idProp))
                {
                    var id = idProp.GetString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        connectionIds.Add(id);
                        countThisPage++;
                    }
                }
            }

            // last page
            if (countThisPage == 0 || data.GetArrayLength() < pageSize)
                break;
        }

        return connectionIds.ToList();
    }

    public async Task<string?> GetUserProfilePictureAsync(string userId, string bearerToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken) || string.IsNullOrWhiteSpace(userId))
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/user/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.TryGetProperty("Data", out var data) && 
                data.TryGetProperty("ProfilePictureUrl", out var picProp))
            {
                return picProp.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Dictionary<string, UserProfilePictureData>> GetBatchUserProfilesAsync(List<string> userIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, UserProfilePictureData>(StringComparer.OrdinalIgnoreCase);

        if (userIds == null || userIds.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("GetBatchUserProfilesAsync: userIds is null or empty");
            return result;
        }

        System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Fetching profiles for {userIds.Count} users: {string.Join(", ", userIds)}");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/batch-profiles");
            
            var jsonContent = JsonSerializer.Serialize(userIds);
            System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Request body: {jsonContent}");
            request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Sending POST request to {_http.BaseAddress}/api/users/batch-profiles");

            using var response = await _http.SendAsync(request, cancellationToken);
            
            System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Response status: {response.StatusCode}");
            
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var responseText = doc.RootElement.GetRawText();
            System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Response body: {responseText}");

            // Get the array - it could be at root or wrapped in "Data"
            JsonElement arrayElement = doc.RootElement;
            
            // If root is an object with a "Data" property that's an array, use that
            if (doc.RootElement.ValueKind == JsonValueKind.Object && 
                doc.RootElement.TryGetProperty("Data", out var dataProperty) && 
                dataProperty.ValueKind == JsonValueKind.Array)
            {
                System.Diagnostics.Debug.WriteLine("GetBatchUserProfilesAsync: Found array in Data property");
                arrayElement = dataProperty;
            }
            // If root is already an array, use it directly
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                System.Diagnostics.Debug.WriteLine("GetBatchUserProfilesAsync: Root element is already an array");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Response is not in expected format. Root type: {doc.RootElement.ValueKind}");
                return result;
            }

            // Parse the array
            var itemCount = 0;
            foreach (var item in arrayElement.EnumerateArray())
            {
                itemCount++;
                // Try both PascalCase and camelCase property names
                var userIdProp = item.TryGetProperty("UserId", out var userIdPascal) ? userIdPascal : 
                                 (item.TryGetProperty("userId", out var userIdCamel) ? userIdCamel : default);
                var nameProp = item.TryGetProperty("Name", out var namePascal) ? namePascal : 
                               (item.TryGetProperty("name", out var nameCamel) ? nameCamel : default);
                var picProp = item.TryGetProperty("ProfilePictureUrl", out var picPascal) ? picPascal : 
                              (item.TryGetProperty("profilePictureUrl", out var picCamel) ? picCamel : default);

                if (userIdProp.ValueKind != JsonValueKind.Undefined &&
                    nameProp.ValueKind != JsonValueKind.Undefined &&
                    picProp.ValueKind != JsonValueKind.Undefined)
                {
                    var userId = userIdProp.GetString();
                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        var name = nameProp.GetString() ?? string.Empty;
                        var picUrl = picProp.GetString() ?? string.Empty;
                        result[userId] = new UserProfilePictureData
                        {
                            Name = name,
                            ProfilePictureUrl = picUrl
                        };
                        System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Added profile for user {userId}: name={name}, pic={picUrl}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Item {itemCount} missing required properties. Available: {item.GetRawText()}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Successfully parsed {result.Count} profiles from {itemCount} items");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Exception occurred: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"GetBatchUserProfilesAsync: Stack trace: {ex.StackTrace}");
            return result;
        }
    }
}

public class UserProfilePictureData
{
    public string Name { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
}