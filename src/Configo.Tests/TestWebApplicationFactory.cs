using System.Data.Common;
using Configo.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Configo.Tests;

public class TestWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    private static int _databaseId;
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddDbContextFactory<ConfigoDbContext>(options =>
            {
                options.UseInMemoryDatabase("ConfigoDb" + Interlocked.Increment(ref _databaseId));
            });
        });

        builder.UseEnvironment("Development");
    }
}
