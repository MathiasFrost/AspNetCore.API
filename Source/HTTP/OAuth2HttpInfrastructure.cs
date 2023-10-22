using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace AspNetCore.API.HTTP;

internal static class OAuth2HttpInfrastructure
{
    private static bool _infrastructureAdded;

    public static IHttpClientBuilder AddOAuth2HttpClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        Action<OAuth2HttpClientOptions> configureOptions)
        where TClient : HttpClient
    {
        if (!_infrastructureAdded)
        {
            services.AddSingleton<AccessTokenProvider>();
            services.AddSingleton<OpenIdConfigurationProvider>();
            _infrastructureAdded = true;
        }

        var o = new OAuth2HttpClientOptions();
        configureOptions(o);

        return services.AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = o.BaseAddress;
                if (o.Timeout.HasValue) client.Timeout = o.Timeout.Value;
                foreach (KeyValuePair<string, IEnumerable<string>> header in o.DefaultRequestHeaders)
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

                if (o.DefaultRequestVersion != null) client.DefaultRequestVersion = o.DefaultRequestVersion;
                if (o.DefaultVersionPolicy.HasValue) client.DefaultVersionPolicy = o.DefaultVersionPolicy.Value;
                if (o.MaxResponseContentBufferSize.HasValue) client.MaxResponseContentBufferSize = o.MaxResponseContentBufferSize.Value;
            })
            .AddHttpMessageHandler(serviceProvider =>
            {
                var accessTokenProvider = serviceProvider.GetRequiredService<AccessTokenProvider>();
                return new OAuth2MessageHandler(o.Authority, o.ClientId, o.Scope, o.ClientSecret, accessTokenProvider);
            });
    }
}

[PublicAPI]
public sealed class OAuth2HttpClientOptions
{
    public string Authority { get; set; } = String.Empty;
    public string ClientSecret { get; set; } = String.Empty;
    public string Scope { get; set; } = String.Empty;
    public string ClientId { get; set; } = String.Empty;
    public long? MaxResponseContentBufferSize { get; set; }
    public HttpVersionPolicy? DefaultVersionPolicy { get; set; }
    public Version? DefaultRequestVersion { get; set; }
    public TimeSpan? Timeout { get; set; }
    public Uri? BaseAddress { get; set; }
    public Dictionary<string, IEnumerable<string>> DefaultRequestHeaders { get; } = new();
}