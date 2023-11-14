using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Configo.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Configo.Server.Program>
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
        builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:Configo", _connectionString },
                    { "Provider", "Postgres" }
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new IntegrationTestLoggerProvider(_outputAccessor));
            });

        builder.UseEnvironment("Development");
    }
}
