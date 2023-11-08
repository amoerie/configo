using Configo.Database;
using Configo.Tests.IntegrationTests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Configo.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly IntegrationTestOutputAccessor _outputAccessor;

    public TestWebApplicationFactory(string connectionString, IntegrationTestOutputAccessor outputAccessor)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _outputAccessor = outputAccessor ?? throw new ArgumentNullException(nameof(outputAccessor));
    }
    
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
            }
            
            services.AddDbContextFactory<ConfigoDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });
        }).ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new IntegrationTestLoggerProvider(_outputAccessor));
        });

        builder.UseEnvironment("Development");
    }
}
