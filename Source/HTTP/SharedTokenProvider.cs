using System.Collections.Concurrent;

namespace AspNetCore.API.HTTP;

public class AccessTokenProvider
{
    private readonly ConcurrentDictionary<string, string> _cachedTokens = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public AccessTokenProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<string> FetchTokenAsync(string scope)
    {
        // Try to retrieve a cached token for the given scope.
        if (_cachedTokens.TryGetValue(scope, out string? cachedToken) && !String.IsNullOrEmpty(cachedToken)) return cachedToken;

        // Ensure only one request at a time per scope by getting/creating a semaphore for the scope.
        SemaphoreSlim semaphore = _semaphores.GetOrAdd(scope, new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();

        try
        {
            // Check again in case another request updated the token while we were waiting for the semaphore.
            if (_cachedTokens.TryGetValue(scope, out cachedToken) && !String.IsNullOrEmpty(cachedToken)) return cachedToken;

            // Create a new HttpClient from the factory
            HttpClient httpClient = _httpClientFactory.CreateClient("oidc");

            // Fetch the token for the given scope (this is a simplified example).
            HttpResponseMessage response = await httpClient.GetAsync($"/token-endpoint?scope={scope}");

            if (response.IsSuccessStatusCode)
            {
                cachedToken = await response.Content.ReadAsStringAsync();
                _cachedTokens[scope] = cachedToken;
            }
            else
            {
                // Handle error
                throw new Exception("Failed to fetch token");
            }
        }
        finally
        {
            semaphore.Release();
        }

        return cachedToken;
    }
}