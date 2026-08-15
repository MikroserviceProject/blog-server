using System.Net.Http;
using System.Text.Json;
using BlogSite.CORE.Interfaces;

namespace BlogSite.CORE.Services
{
    public class SocialServiceClient : ISocialServiceClient
    {
        private readonly HttpClient _httpClient;
        
        // This expects a named HttpClient "SocialApi" configured in Program.cs
        public SocialServiceClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("SocialApi");
        }

        public async Task<List<Guid>> GetFollowersAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/social/followers-ids/{userId}");
                if (!response.IsSuccessStatusCode)
                    return new List<Guid>();

                var content = await response.Content.ReadAsStringAsync();
                
                // Assuming Social.API returns: { "success": true, "data": [ "guid1", "guid2" ] }
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    if (root.TryGetProperty("data", out var dataElement))
                    {
                        var list = JsonSerializer.Deserialize<List<Guid>>(dataElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (list != null) return list;
                    }
                }
                
                return new List<Guid>();
            }
            catch
            {
                // In a real scenario, log the error
                return new List<Guid>();
            }
        }
    }
}
