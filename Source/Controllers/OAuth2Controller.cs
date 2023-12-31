﻿using AspNetCore.API.HTTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]/[action]")]
public sealed class OAuth2Controller : ControllerBase
{
    private readonly IDataProtector _dataProtector;
    private readonly TestHttp _testHttp;

    public OAuth2Controller(TestHttp testHttp, IDataProtectionProvider dataProtectionProvider)
    {
        _testHttp = testHttp;
        _dataProtector = dataProtectionProvider.CreateProtector("JwtCookie");
    }

    [HttpGet, Authorize]
    public IDictionary<string, string> Claims() => User.Claims.ToDictionary(static claim => claim.Type, static claim => claim.Value);

    [HttpGet]
    public async Task<IDictionary<string, string>?> Invoke(CancellationToken token) => await _testHttp.GetClaims(token);

    [HttpGet("{name:required}")]
    public void SetItem(string name, [FromQuery] string jwtBearer)
    {
        Response.Cookies.Delete(name);
        Response.Cookies.Append(name, _dataProtector.Protect(jwtBearer), new CookieOptions {
            Domain = "localhost",
            Secure = false,
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            MaxAge = TimeSpan.FromHours(1),
            IsEssential = true
        });
    }

    [HttpGet("{name:required}")]
    public string? GetItem(string name) => Request.Cookies.TryGetValue(name, out string? res) ? _dataProtector.Unprotect(res) : null;
}