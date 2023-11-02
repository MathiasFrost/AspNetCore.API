namespace AspNetCore.API.HTTP;

public sealed class TestHttp : HttpClient
{
    private readonly HttpClient _httpClient;

    public TestHttp(HttpClient httpClient) => _httpClient = httpClient;

    internal async Task<IDictionary<string, string>?> GetClaims(CancellationToken token) =>
        await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "OAuth2/Claims"), token).GetJsonContent<IDictionary<string, string>>();
}

internal static class HttpExtensions
{
    public static async Task<TResult> GetJsonContent<TResult>(this Task<HttpResponseMessage> request)
    {
        HttpResponseMessage res = await request;
        return await res.Content.ReadFromJsonAsync<TResult>();
    }
}