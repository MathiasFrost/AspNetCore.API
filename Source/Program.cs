using System.Net;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using AspNetCore.API.Contracts;
using AspNetCore.API.Database;
using AspNetCore.API.HTTP;
using AspNetCore.API.Hubs;
using AspNetCore.API.Python;
using AspNetCore.API.Schemas;
using AspNetCore.API.TCP;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using GraphQL;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
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

builder.Services.AddRateLimiter(static options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(static partitioner =>
    {
        return new RateLimitPartition<IPAddress>(partitioner.Connection.RemoteIpAddress ?? IPAddress.Any, static _ =>
            new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions {
                PermitLimit = 4,
                QueueLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromSeconds(12)
            }));
    });
    options.AddFixedWindowLimiter("EX", static context =>
    {
        context.PermitLimit = 4;
        context.Window = TimeSpan.FromSeconds(12);
        context.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        context.QueueLimit = 2;
    });
});

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

builder.Services.AddScoped<AspNetCoreDb>();

builder.Services.AddGrpc(static options => options.EnableDetailedErrors = true);

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddTransient<WeatherForecastService>();

builder.Services.AddGraphQL(static qlBuilder =>
{
    qlBuilder.AddSystemTextJson();
    qlBuilder.AddSchema<WeatherForecastSchema>();
});

builder.Services.AddSignalR();

builder.Services.AddHostedService<TestHostedService>();

builder.Services.AddHangfireServer(static options => options.WorkerCount = 1);
builder.Services.AddHangfire(configuration => configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(builder.Configuration.GetConnectionString("AspNetCore.DB"), new MySqlStorageOptions())));

builder.Services.AddTransient<WeatherForecastAnalysis>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

// Request filtering
app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Additional UI
app.UseSwagger();
app.UseSwaggerUI();
app.UseGraphQLPlayground();
app.UseHangfireDashboard(options: new DashboardOptions{StatsPollingInterval = 200});

// Transport protocols
app.MapControllers();
app.UseGraphQL<WeatherForecastSchema>(configureMiddleware: static options => options.AuthorizationRequired = true);
app.MapHub<WeatherForecastHub>("/WeatherForecast");
app.MapGrpcService<AspNetCore.API.Services.WeatherForecastService>().RequireAuthorization();
app.UseServiceModel(serviceBuilder =>
{
    // Add the Echo Service
    serviceBuilder.AddService<WeatherForecastService>();
    serviceBuilder.AddServiceEndpoint<WeatherForecastService, IWeatherForecastService>(new WSHttpBinding(SecurityMode.None), "/WeatherForecastService.svc");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

RecurringJob.AddOrUpdate<WeatherForecastAnalysis>(nameof(WeatherForecastAnalysis), static job => job.Run(), "1 * * * *");

app.Run();