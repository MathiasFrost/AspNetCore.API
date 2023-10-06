using AspNetCore.API.HTTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]/[action]")]
public sealed class OAuth2Controller : ControllerBase
{
    private readonly TestHttp _testHttp;

    public OAuth2Controller(TestHttp testHttp) => _testHttp = testHttp;

    [HttpGet, Authorize]
    public IDictionary<string, string> Claims() => User.Claims.ToDictionary(static claim => claim.Type, static claim => claim.Value);

    [HttpGet]
    public async Task<IDictionary<string, string>?> Invoke(CancellationToken token) => await _testHttp.GetClaims(token);
}