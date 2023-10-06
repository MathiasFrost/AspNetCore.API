using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]/[action]"), Authorize]
public sealed class OAuth2Controller : ControllerBase
{
    [HttpGet]
    public IDictionary<string, string> Claims() => User.Claims.ToDictionary(claim => claim.Type, claim => claim.Value);
}