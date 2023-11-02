using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.API.Test;

public class UnitTest1
{
    public UnitTest1()
    {
        var host = new WebApplicationFactory<Program>();
        IServiceScope scope = host.Services.CreateScope();
    }
    
    [Fact]
    public void Test1() { }
}