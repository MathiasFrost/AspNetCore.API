using System.Net.Http.Headers;
using AspNetCore.API.Contracts;
using AspNetCore.API.HTTP;
using AspNetCore.API.Hubs;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(static configuration => configuration.RegisterServicesFromAssemblyContaining<Program>());

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
        foreach (KeyValuePair<string, string> pair in context.Request.Cookies)
        {
            if (!pair.Key.EndsWith("AspNetCore.API_AccessToken")) continue;
            try
            {
                IDataProtector dp = context.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("JwtCookie");
                context.Request.Headers.Remove(HeaderNames.Authorization);
                var authHeader = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, dp.Unprotect(pair.Value));
                context.Request.Headers.Authorization = authHeader.ToString();
                return "Browser"; // only for testing
            }
            catch (Exception e)
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtCookie").LogWarning(e, "Invalid encrypted JWT token");
            }
        }

        return StringValues.IsNullOrEmpty(context.Request.Headers.Origin) ? "Daemon" : "Browser";
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

builder.Services.AddGrpc(static options => options.EnableDetailedErrors = true);

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddTransient<WeatherForecastService>();

builder.Services.AddSignalR();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();

app.MapHub<WeatherForecastHub>("/WeatherForecast");
app.MapControllers();
app.MapGrpcService<AspNetCore.API.Services.WeatherForecastService>();
app.UseServiceModel(serviceBuilder =>
{
    // Add the Echo Service
    serviceBuilder.AddService<WeatherForecastService>();
    serviceBuilder.AddServiceEndpoint<WeatherForecastService, IWeatherForecastService>(new WSHttpBinding(SecurityMode.None), "/WeatherForecastService.svc");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

app.Run();