using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AspNetCore.API.HTTP;

public sealed class AccessTokenProvider
{
    private readonly ConcurrentDictionary<string, string> _cachedTokens = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenIdConfigurationProvider _openIdConfigurationProvider;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public AccessTokenProvider(IHttpClientFactory httpClientFactory, OpenIdConfigurationProvider openIdConfigurationProvider)
    {
        _httpClientFactory = httpClientFactory;
        _openIdConfigurationProvider = openIdConfigurationProvider;
    }

    public async Task<string> FetchTokenAsync(string authority, string clientId, string clientSecret, string scope, CancellationToken token)
    {
        // Try to retrieve a cached token for the given scope.
        if (_cachedTokens.TryGetValue(scope, out string? cachedToken) && !String.IsNullOrEmpty(cachedToken)) return cachedToken;

        // Ensure only one request at a time per scope by getting/creating a semaphore for the scope.
        SemaphoreSlim semaphore = _semaphores.GetOrAdd(scope, new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(token);

        try
        {
            // Check again in case another request updated the token while we were waiting for the semaphore.
            if (_cachedTokens.TryGetValue(scope, out cachedToken) && !String.IsNullOrEmpty(cachedToken)) return cachedToken;

            // Create a new HttpClient from the factory
            HttpClient httpClient = _httpClientFactory.CreateClient(authority);

            // Fetch the token for the given scope (this is a simplified example).
            var data = new KeyValuePair<string, string>[] {
                new(OpenIdConnectParameterNames.ClientId, clientId),
                new(OpenIdConnectParameterNames.ClientSecret, clientSecret),
                new(OpenIdConnectParameterNames.Scope, scope),
                new(OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials)
            };
            OpenIdConnectConfiguration config = await _openIdConfigurationProvider.GetConfigurationAsync(authority, token);
            HttpResponseMessage response = await httpClient.PostAsync(config.TokenEndpoint, new FormUrlEncodedContent(data), token);

            if (response.IsSuccessStatusCode)
            {
                cachedToken = await response.Content.ReadAsStringAsync(token);
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