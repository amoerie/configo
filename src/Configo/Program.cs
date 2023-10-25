using System.IO.Compression;
using Configo.Data;
using Configo.Database;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var host = builder.Host;

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
    dbContextOptions.UseSqlServer(configuration.GetConnectionString("ConfigoDb"));
});
services.AddHostedService<DatabaseMigrator>();

services.AddSingleton<WeatherForecastService>();

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
