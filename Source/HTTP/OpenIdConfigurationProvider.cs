using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AspNetCore.API.HTTP;

public class OpenIdConfigurationProvider
{
    private readonly ConcurrentDictionary<string, OpenIdConnectConfiguration> _configurations = new();
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenIdConfigurationProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string issuer)
    {
        // Try to get cached configuration
        if (_configurations.TryGetValue(issuer, out OpenIdConnectConfiguration? cachedConfig)) return cachedConfig;

        // Fetch new configuration
        HttpClient httpClient = _httpClientFactory.CreateClient();
        var discoveryDocumentUri = $"{issuer}/.well-known/openid-configuration";
        string response = await httpClient.GetStringAsync(discoveryDocumentUri);
        var config = new OpenIdConnectConfiguration(response);

        // Cache the fetched configuration
        _configurations[issuer] = config;

        return config;
    }
}