using System.IO.Compression;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Configo.Database;
using Configo.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
services.AddBlazorise(o => { o.Immediate = true; });
services.AddBootstrap5Providers();
services.AddFontAwesomeIcons();

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

    dbContextOptions.UseSqlServer(configuration.GetConnectionString("ConfigoDb"));
});
services.AddHostedService<DatabaseMigrator>();

// Domain
services.AddSingleton<TagGroupManager>();
services.AddSingleton<TagManager>();
services.AddSingleton<ApplicationManager>();
services.AddSingleton<ApiKeyManager>();
services.AddSingleton<ApiKeyGenerator>();
services.AddSingleton<SchemaManager>();

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

// Routing
// ---------
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Let's go
// --------
app.Run();
