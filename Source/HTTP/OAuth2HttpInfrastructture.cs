using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace AspNetCore.API.HTTP;

internal static class OAuth2HttpInfrastructure
{
    private static readonly ICollection<string> AddedAuthorities = new List<string>();

    public static IHttpClientBuilder AddOAuth2HttpClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        Action<OAuth2HttpClientOptions> configureOptions)
        where TClient : HttpClient
    {
        if (AddedAuthorities.Count == 0)
        {
            services.AddSingleton<AccessTokenProvider>();
            services.AddSingleton<OpenIdConfigurationProvider>();
        }


        var o = new OAuth2HttpClientOptions();
        configureOptions(o);

        if (!AddedAuthorities.Any(s => s == o.Authority))
        {
            services.AddHttpClient(o.Authority, client =>
            {
                client.BaseAddress = new Uri(o.Authority);
                client.Timeout = TimeSpan.FromSeconds(3);
            });
            AddedAuthorities.Add(o.Authority);
        }

        return services.AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = o.BaseAddress;
                if (o.Timeout.HasValue) client.Timeout = o.Timeout.Value;
                // foreach (KeyValuePair<string, IEnumerable<string>> pair in o.DefaultRequestHeaders)
                // {
                // client.DefaultRequestHeaders.Add(pair.Key, pair.Value);
                // }

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

public sealed class OAuth2HttpClientOptions
{
    public string Authority { get; set; } = String.Empty;
    public long? MaxResponseContentBufferSize { get; set; }
    public HttpVersionPolicy? DefaultVersionPolicy { get; set; }
    public Version? DefaultRequestVersion { get; set; }
    public TimeSpan? Timeout { get; set; }
    public Uri? BaseAddress { get; set; }
    public HttpRequestHeaders? DefaultRequestHeaders { get; set; }
    public string ClientSecret { get; set; } = String.Empty;
    public string Scope { get; set; } = String.Empty;
    public string ClientId { get; set; } = String.Empty;
}