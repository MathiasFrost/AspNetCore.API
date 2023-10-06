namespace AspNetCore.API.HTTP;

public sealed class TestHttp : HttpClient
{
    private readonly HttpClient _httpClient;

    public TestHttp(HttpClient httpClient) => _httpClient = httpClient;

    internal async Task<IDictionary<string, string>?> GetClaims(CancellationToken token) =>
        await _httpClient.GetFromJsonAsync<IDictionary<string, string>>("OAuth2/Claims", token);
}