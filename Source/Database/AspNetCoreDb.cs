namespace AspNetCore.API.Database;

public sealed class AspNetCoreDb : DatabaseIo
{
    public AspNetCoreDb(IConfiguration configuration) : base(configuration.GetConnectionString("AspNetCore.DB")!) { }
}