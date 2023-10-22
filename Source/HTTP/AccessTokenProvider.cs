using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AspNetCore.API.HTTP;

public sealed class AccessTokenProvider
{
    private static short _fetches;
    private readonly ConcurrentDictionary<string, string> _cachedTokens = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenIdConfigurationProvider _openIdConfigurationProvider;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public AccessTokenProvider(IHttpClientFactory httpClientFactory, OpenIdConfigurationProvider openIdConfigurationProvider)
    {
        _httpClientFactory = httpClientFactory;
        _openIdConfigurationProvider = openIdConfigurationProvider;
    }

    internal async Task<string> FetchTokenAsync(string authority, string clientId, string clientSecret, string scope, bool forceFetch, CancellationToken token)
    {
        // Try to retrieve a cached token for the given scope.
        if (!forceFetch && _cachedTokens.TryGetValue(scope, out string? cachedToken) && !String.IsNullOrEmpty(cachedToken)) return cachedToken;

        // Ensure only one request at a time per scope by getting/creating a semaphore for the scope.
        SemaphoreSlim semaphore = _semaphores.GetOrAdd(scope, new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(token);

        try
        {
            // Check again in case another request updated the token while we were waiting for the semaphore.
            if (!forceFetch && _cachedTokens.TryGetValue(scope, out cachedToken) && !String.IsNullOrEmpty(cachedToken)) return cachedToken;

            // Create a new HttpClient from the factory
            HttpClient httpClient = _httpClientFactory.CreateClient();

            // Fetch the token for the given scope (this is a simplified example).
            var data = new KeyValuePair<string, string>[] {
                new(OpenIdConnectParameterNames.ClientId, clientId),
                new(OpenIdConnectParameterNames.ClientSecret, clientSecret),
                new(OpenIdConnectParameterNames.Scope, scope),
                new(OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials)
            };
            OpenIdConnectConfiguration config = await _openIdConfigurationProvider.GetConfigurationAsync(authority, token);
            HttpResponseMessage response = await httpClient.PostAsync(config.TokenEndpoint, new FormUrlEncodedContent(data), token);
            _fetches++;
            Console.WriteLine($"#########################\n{_fetches}\n######################");

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = new OpenIdConnectMessage(await response.Content.ReadAsStringAsync(token));
                // Store the whole message or a struct with expires_in and access_token and do a check for expiry at the start of this method
                cachedToken = tokenResponse.AccessToken;
                _cachedTokens[scope] = cachedToken;
            }
            else
            {
                // Handle error
                throw new Exception($"Failed to fetch token: {await response.Content.ReadAsStringAsync(token)}");
            }
        }
        finally
        {
            semaphore.Release();
        }

        return cachedToken;
    }
}