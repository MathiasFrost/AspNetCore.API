using System.Net;
using System.Net.Http.Headers;

namespace AspNetCore.API.HTTP;

public class OAuth2MessageHandler : DelegatingHandler
{
    private string _accessToken = String.Empty;

    public OAuth2MessageHandler(IConfiguration configuration, HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Attach access token to headers
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        // Send the request
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)) return response;

        // Refresh the token
        _accessToken = await RefreshAccessToken();

        // Clone the request and attach the new token
        HttpRequestMessage newRequest = await CloneHttpRequestMessageAsync(request);
        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        // Retry the request
        response = await base.SendAsync(newRequest, cancellationToken);

        // Optionally, give up if it still fails
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Handle the error
        }

        return response;
    }

    private async Task<string> RefreshAccessToken() =>
        // Logic to refresh the token
        "new_access_token";

    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
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