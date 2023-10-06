using AspNetCore.API.HTTP;

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

builder.Services.AddAuthentication("Default")
    .AddJwtBearer("Default", static options =>
    {
        options.Authority = "https://login.microsoft.com";
        options.Audience = "";
    });

builder.Services.AddTransient<OAuth2MessageHandler>();
builder.Services.AddHttpClient<TestHttpClient>(static client =>
    {
        client.BaseAddress = new Uri("http://localhost:5000/");
        client.Timeout = TimeSpan.FromSeconds(6);
    })
    .AddHttpMessageHandler<OAuth2MessageHandler>();

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