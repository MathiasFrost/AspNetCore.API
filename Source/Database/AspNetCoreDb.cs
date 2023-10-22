namespace AspNetCore.API.Database;

public sealed class AspNetCoreDb : DatabaseMySql
{
    public AspNetCoreDb(IConfiguration configuration) : base(configuration.GetConnectionString("AspNetCore.DB")!) { }
}