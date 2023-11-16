using System.IO.Compression;
using Configo.Database;
using Configo.Database.NpgSql;
using Configo.Database.SqlServer;
using Configo.Server.Database;
using Configo.Server.Domain;
using Configo.Server.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

// Dependency Injection
// --------------------
var services = builder.Services;

// Web
services.AddRazorPages();
services.AddServerSideBlazor();

// Theming
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
services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, "Configo API Key", _ => {});

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

    dbContextOptions.ConfigureWarnings(warnings =>
    {
        warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
    });

    /* Support both SQL server and Postgres */
    var provider = configuration.GetValue("Provider", "SqlServer");

    switch (provider)
    {
        case "SqlServer":
            dbContextOptions.UseSqlServer(configuration.GetConnectionString("Configo"), sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(SqlServerMigrations).Assembly.GetName().Name);
            });
            break;
        case "Postgres":
            dbContextOptions.UseNpgsql(configuration.GetConnectionString("Configo"), npgSqlOptions =>
            {
                npgSqlOptions.MigrationsAssembly(typeof(NpgSqlMigrations).Assembly.GetName().Name);
            });
            break;
        default:
            throw new InvalidOperationException("Unsupported database provider, only SqlServer and Postgres are supported: " + provider);
    }
});
services.AddHostedService<DatabaseMigrator>();

// Domain
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

var app = builder.Build();

// Middleware
// ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Note: don't enforce HTTPS/HSTS to support SSL offloading

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();


// Routing
// ---------
app.MapBlazorHub();
var api = app.MapGroup("/api");

api.MapGet("/config", GetConfigEndpoint.HandleAsync)
    .RequireAuthorization(authorizationPolicyBuilder =>
    {
        authorizationPolicyBuilder.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.AuthenticationScheme);
        authorizationPolicyBuilder.RequireClaim(ApiKeyAuthenticationHandler.ApiKeyIdClaim);
    });
api.MapPost("/applications/{applicationId}/schema", SaveSchemaEndpoint.HandleAsync);

app.MapFallbackToPage("/_Host");

// Let's go
// --------
app.Run();

// Expose for integration tests
namespace Configo.Server
{
    public partial class Program { }
}
