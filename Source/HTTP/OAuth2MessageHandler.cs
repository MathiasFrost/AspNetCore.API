using System.Net;
using System.Net.Http.Headers;

namespace AspNetCore.API.HTTP;

internal sealed class OAuth2MessageHandler : DelegatingHandler
{
    private readonly AccessTokenProvider _accessTokenProvider;
    private readonly string _authority;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;

    public OAuth2MessageHandler(string authority,
        string clientId,
        string scope,
        string clientSecret,
        AccessTokenProvider accessTokenProvider)
    {
        _authority = authority;
        _clientId = clientId;
        _scope = scope;
        _clientSecret = clientSecret;
        _accessTokenProvider = accessTokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        // Attach access token to headers
        string accessToken = await _accessTokenProvider.FetchTokenAsync(_authority, _clientId, _clientSecret, _scope, false, token);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Send the request
        HttpResponseMessage response = await base.SendAsync(request, token);

        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)) return response;

        Console.WriteLine("Re-invoking");

        // Refresh the token
        accessToken = await _accessTokenProvider.FetchTokenAsync(_authority, _clientId, _clientSecret, _scope, true, token);

        // Clone the request and attach the new token
        HttpRequestMessage newRequest = await CloneHttpRequestMessageAsync(request);
        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Retry the request
        response = await base.SendAsync(newRequest, token);

        // Optionally, give up if it still fails
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // Handle the error
        }
        else if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
        {
            // Do some retries
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        // Copy the headers
        foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        // Copy the content (if applicable)
        if (req.Content != null)
        {
            // Be careful with streams here; you might need to reset their positions or clone them
            clone.Content = new ByteArrayContent(await req.Content.ReadAsByteArrayAsync());

            // Copy content headers
            foreach (KeyValuePair<string, IEnumerable<string>> h in req.Content.Headers) clone.Content.Headers.Add(h.Key, h.Value);
        }

        return clone;
    }
}