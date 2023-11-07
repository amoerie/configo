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
            // Prune services we don't need for integration tests
            for (int i = services.Count - 1; i >= 0; i--)
            {
                var service = services[i];
                
                // Remove existing EF Core stuff, we'll add it anew
                if (service.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                {
                    services.RemoveAt(i);
                }
                // Remove automatic database migration while we use in memory EF
                else if (service.ImplementationType == typeof(DatabaseMigrator))
                {
                    services.RemoveAt(i);
                }
            }
            services.AddDbContextFactory<ConfigoDbContext>(options =>
            {
                options.UseInMemoryDatabase("ConfigoDb" + Interlocked.Increment(ref _databaseId));
            });
        });

        builder.UseEnvironment("Development");
    }
}
