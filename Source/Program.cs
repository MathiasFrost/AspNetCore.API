using AspNetCore.API.HTTP;
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

builder.Services.AddAuthentication("Default")
    .AddJwtBearer("Default", options =>
    {
        options.Authority = authority;
        options.Audience = clientId;
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = false,
            IssuerValidator = (issuer, token, parameters) => { return token.Issuer; }
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