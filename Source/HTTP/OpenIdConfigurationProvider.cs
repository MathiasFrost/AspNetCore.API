using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AspNetCore.API.HTTP;

public sealed class OpenIdConfigurationProvider
{
    private readonly ConcurrentDictionary<string, OpenIdConnectConfiguration> _configurations = new();
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenIdConfigurationProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string authority, CancellationToken token)
    {
        // Try to get cached configuration
        if (_configurations.TryGetValue(authority, out OpenIdConnectConfiguration? cachedConfig)) return cachedConfig;

        // Fetch new configuration
        HttpClient httpClient = _httpClientFactory.CreateClient(authority);
        string response = await httpClient.GetStringAsync("/.well-known/openid-configuration", token);
        var config = new OpenIdConnectConfiguration(response);

        // Cache the fetched configuration
        _configurations[authority] = config;

        return config;
    }
}