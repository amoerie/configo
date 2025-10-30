using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;

namespace Configo.Tests;

public class TestWebApplicationFactory(NpgsqlConnectionStringBuilder dbConnectionString, ConfigurationOptions redisConnectionString, IntegrationTestOutputAccessor outputAccessor)
    : WebApplicationFactory<Configo.Server.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:Configo", dbConnectionString.ConnectionString },
                    { "ConnectionStrings:Redis", redisConnectionString.ToString() },
                    { "Provider", "Postgres" }
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new IntegrationTestLoggerProvider(outputAccessor));
            }).ConfigureServices(services =>
            {
                // Test specific authentication
                services.AddHostedService<RemoveUnsupportedAuthenticationSchemesHostedService>();
                services.AddAuthentication(o =>
                    {
                        o.DefaultScheme = TestAuthenticationHandler.AuthenticationScheme;
                        o.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                    })
                    .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme,
                        options =>
                        {
                            options.Authenticate = () =>
                            {
                                List<Claim> claims =
                                [
                                    new Claim(ClaimTypes.GivenName, "John"),
                                    new Claim(ClaimTypes.Surname, "Doe"),
                                    new Claim(ClaimTypes.Name, "John Doe"),
                                    new Claim(ClaimTypes.Email, "john.doe@configo.com")
                                ];
                                var claimsIdentity = new ClaimsIdentity(claims, TestAuthenticationHandler.AuthenticationScheme);
                                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                                var authenticationTicket = new AuthenticationTicket(claimsPrincipal, TestAuthenticationHandler.AuthenticationScheme);
                                var authenticateResult = AuthenticateResult.Success(authenticationTicket);
                                return Task.FromResult(authenticateResult);
                            };
                        });
            });

        builder.UseEnvironment("Development");
    }
}
