using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AspNetCore.API.Test;

public sealed class UnitTest1
{
    private readonly HttpClient _client;

    public UnitTest1()
    {
        var host = new WebApplicationFactory<Program>();
        _client = host.CreateDefaultClient();
    }

    [Theory, InlineData("World/All")]
    public async Task Call_WorldAll_ReturnArray(string uri)
    {
        HttpResponseMessage res = await _client.GetAsync(uri);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}