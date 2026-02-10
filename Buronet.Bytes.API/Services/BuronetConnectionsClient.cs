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
}