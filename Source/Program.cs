using AspNetCore.API.HTTP;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Admin UI
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddCors(static options => options.AddDefaultPolicy(static policyBuilder =>
{
    policyBuilder.WithOrigins("http://localhost:5173");
    policyBuilder.AllowAnyHeader();
    policyBuilder.AllowAnyMethod();
    policyBuilder.AllowCredentials();
}));

IConfigurationSection externalApi = builder.Configuration.GetSection("ExternalAPI");
IConfigurationSection test = externalApi.GetSection("Test");
var baseAddress = test.GetValue<string>("BaseAddress")!;
var scope = test.GetValue<string>("Scope")!;
var timeoutSeconds = externalApi.GetValue<double>("TimeoutSeconds");

var oidcProvider = externalApi.GetValue<string>("OIDC")!;
IConfigurationSection oidc = builder.Configuration.GetSection("OIDC");
IConfigurationSection provider = oidc.GetSection(oidcProvider);

var tenantId = provider.GetValue<string>("TenantId")!;
string authority = String.Format(provider.GetValue<string>("Authority")!, tenantId);
var clientId = provider.GetValue<string>("ClientId")!;
var clientSecret = provider.GetValue<string>("ClientSecret")!;

builder.Services.AddAuthentication("Dynamic")
    .AddPolicyScheme("Dynamic", "Dynamic Scheme", static options => options.ForwardDefaultSelector = static context =>
    {
        if (!context.Request.Cookies.TryGetValue("JWT", out string? encryptedJwt))
            return StringValues.IsNullOrEmpty(context.Request.Headers.Origin) ? "Daemon" : "Browser";

        try
        {
            IDataProtector dp = context.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("JwtCookie");
            context.Request.Headers.Remove("Authorization");
            context.Request.Headers.Authorization = dp.Unprotect(encryptedJwt);
        }
        catch (Exception e)
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtCookie").LogWarning(e, "Invalid encrypted JWT token");
        }

        return "Daemon"; // only for testing
    })
    .AddJwtBearer("Daemon", null, options =>
    {
        options.Authority = authority;
        options.Audience = clientId;
    })
    .AddJwtBearer("Browser", null, options =>
    {
        options.Authority = "https://login.microsoftonline.com/common/v2.0";
        options.Audience = clientId;
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = false
        };
    });

builder.Services.AddOAuth2HttpClient<TestHttp>(options =>
{
    options.BaseAddress = new Uri(baseAddress);
    options.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    options.Authority = authority;
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.Scope = scope;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();

app.MapBlazorHub();
app.MapControllers();

app.Run();