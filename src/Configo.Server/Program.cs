using System.IO.Compression;
using Configo.Database;
using Configo.Database.NpgSql;
using Configo.Database.SqlServer;
using Configo.Server;
using Configo.Server.Components;
using Configo.Server.Database;
using Configo.Server.Domain;
using Configo.Server.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var host = builder.Host;
var environment = builder.Environment;

// Host
// ----
host.UseDefaultServiceProvider(o =>
{
    o.ValidateScopes = true;
    o.ValidateOnBuild = true;
});

// Configuration
configuration.AddEnvironmentVariables("CONFIGO_");

// Dependency Injection
// --------------------
var services = builder.Services;

// Web
services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor Theming
services.AddMudServices();

// Reverse proxy support
services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Compression support
services.AddResponseCompression(o => o.EnableForHttps = true);
services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// Data Protection
services.AddDataProtection()
    .SetApplicationName("Configo")
    .PersistKeysToDbContext<ConfigoDbContext>();

// Authentication
services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = MicrosoftAccountDefaults.AuthenticationScheme;
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, "Configo API Key", _ => { })
    .AddCookie()
    .AddMicrosoftAccount();

services.AddOptions<ConfigoAuthenticationOptions>().BindConfiguration(ConfigoAuthenticationOptions.SectionName);
services.AddOptions<MicrosoftAccountOptions>(MicrosoftAccountDefaults.AuthenticationScheme)
    .Configure((MicrosoftAccountOptions options, IOptions<ConfigoAuthenticationOptions> configoAuthenticationOptions) =>
    {
        MicrosoftOptions microsoftOptions = configoAuthenticationOptions.Value.Microsoft;
        if (microsoftOptions.TenantId is not null)
        {
            options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{microsoftOptions.TenantId}/oauth2/authorize";
            options.TokenEndpoint = $"https://login.microsoftonline.com/{microsoftOptions.TenantId}/oauth2/v2.0/token";
        }
        options.ClientId = microsoftOptions.ClientId;
        options.ClientSecret = microsoftOptions.ClientSecret;
    });

/* Authorization */
var requireAuthenticatedUserPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
services.AddAuthorizationBuilder().SetFallbackPolicy(requireAuthenticatedUserPolicy);


// SQL Server Database
services.AddDbContextFactory<ConfigoDbContext>(dbContextOptions =>
{
    if (environment.IsDevelopment())
    {
        /*
         * For performance reasons, EF Core does not wrap each call to read a value from the database provider in a try-catch block.
         * However, this sometimes results in exceptions that are hard to diagnose, especially when the database returns a NULL when not allowed by the model.
         * Turning on EnableDetailedErrors will cause EF to introduce these try-catch blocks and thereby provide more detailed errors.
         */
        dbContextOptions.EnableDetailedErrors();

        /*
         * By default, EF Core will not include the values of any data in exception messages.
         * This is because such data may be confidential, and could be revealed in production use if an exception is not handled.
         * However, knowing data values, especially for keys, can be very helpful when debugging
         */
        dbContextOptions.EnableSensitiveDataLogging();
    }

    /*
     * For performance reasons, don't track loaded entities by default
     */
    dbContextOptions.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    dbContextOptions.ConfigureWarnings(warnings => { warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning); });

    /* Support both SQL server and Postgres */
    var provider = configuration.GetValue("Provider", "SqlServer");

    switch (provider)
    {
        case "SqlServer":
            dbContextOptions.UseSqlServer(configuration.GetConnectionString("Configo"),
                sqlServerOptions => { sqlServerOptions.MigrationsAssembly(typeof(SqlServerMigrations).Assembly.GetName().Name); });
            break;
        case "Postgres":
            dbContextOptions.UseNpgsql(configuration.GetConnectionString("Configo"),
                npgSqlOptions => { npgSqlOptions.MigrationsAssembly(typeof(NpgSqlMigrations).Assembly.GetName().Name); });
            break;
        default:
            throw new InvalidOperationException("Unsupported database provider, only SqlServer and Postgres are supported: " + provider);
    }
});
services.AddHostedService<DatabaseMigratorHostedService>();

// Domain
services.AddSingleton<UserManager>();
services.AddSingleton<TagGroupManager>();
services.AddSingleton<TagManager>();
services.AddSingleton<ApplicationManager>();
services.AddSingleton<ApiKeyManager>();
services.AddSingleton<ApiKeyGenerator>();
services.AddSingleton<SchemaManager>();
services.AddSingleton<VariableManager>();
services.AddSingleton<VariablesJsonSerializer>();
services.AddSingleton<VariablesJsonDeserializer>();
services.AddSingleton<VariablesPendingChanges>();

// Aspire
builder.AddServiceDefaults();

var app = builder.Build();

// Middleware
// ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// Routing
// ---------
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

var api = app.MapGroup("/api");
api.MapGet("/config", GetConfigEndpoint.HandleAsync).RequireAuthorization(auth =>
{
    auth.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.AuthenticationScheme);
    auth.RequireClaim(ApiKeyAuthenticationHandler.ApiKeyIdClaim);
});
api.MapPost("/applications/{applicationId}/schema", SaveSchemaEndpoint.HandleAsync);

// Let's go
// --------
app.Run();

// Expose for integration tests
namespace Configo.Server
{
    public partial class Program { }
}
