using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace Configo.Tests;

[CollectionDefinition(IntegrationTestFixture.Collection)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public const string Collection = "IntegrationTests";

    private PostgreSqlContainer _container = null!;
    private Respawner _respawner = null!;
    private TestWebApplicationFactory _testWebApplicationFactory = null!;
    private string _connectionString = null!;
    private readonly IntegrationTestOutputAccessor _outputAccessor = new IntegrationTestOutputAccessor();
    private ILogger<IntegrationTestFixture>? _logger;

    public async Task InitializeAsync()
    {
        // setup database
        _container = new PostgreSqlBuilder()
            .WithDatabase("configo")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        _connectionString = new NpgsqlConnectionStringBuilder(_connectionString) { IncludeErrorDetail = true }.ConnectionString;

        _testWebApplicationFactory = new TestWebApplicationFactory(_connectionString, _outputAccessor);
        
        // By using any of the APIs, we ensure that the server is running
        using var _ = _testWebApplicationFactory.CreateClient();

        _logger = _testWebApplicationFactory.Services.GetRequiredService<ILogger<IntegrationTestFixture>>();
        _logger.LogInformation("Creating database respawn point");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude =
            [
                "public"
            ],
            DbAdapter = DbAdapter.Postgres
        });
        
        _logger.LogInformation("Initialization complete");
    }

    public T GetRequiredService<T>() where T : notnull => _testWebApplicationFactory.Services.GetRequiredService<T>();
    public HttpClient CreateClient() => _testWebApplicationFactory.CreateClient();

    public void SetOutput(ITestOutputHelper output)
    {
        _outputAccessor.Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public async Task ResetAsync()
    {
        _logger?.LogInformation("Resetting database");
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        _outputAccessor.Output = null;
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _container.StopAsync();
            await _testWebApplicationFactory.DisposeAsync();
        }
        finally
        {
            await _container.DisposeAsync();
        }
    }
}

public sealed class IntegrationTestOutputAccessor
{
    public ITestOutputHelper? Output { get; set; }
}

public sealed class IntegrationTestLoggerProvider : ILoggerProvider
{
    private readonly IntegrationTestOutputAccessor _outputAccessor;

    public IntegrationTestLoggerProvider(IntegrationTestOutputAccessor outputAccessor)
    {
        _outputAccessor = outputAccessor ?? throw new ArgumentNullException(nameof(outputAccessor));
    }

    public ILogger CreateLogger(string categoryName)
    {
        var logger = new IntegrationTestLogger(_outputAccessor);
        logger.SetMinimumLevel(LogLevel.Trace);
        logger.IncludeTimestamps();
        logger.IncludeThreadId();
        logger.IncludePrefix(categoryName);
        return logger;
    }

    public void Dispose() { }
}

public class IntegrationTestLogger : ILogger
{
    private delegate string PrefixEnricher(string prefix);

    private readonly List<PrefixEnricher> _prefixEnrichers;
    private readonly IntegrationTestOutputAccessor _outputAccessor;
    private LogLevel _minimumLevel;

    public IntegrationTestLogger(IntegrationTestOutputAccessor outputAccessor) : this(outputAccessor, LogLevel.Debug, new List<PrefixEnricher>()) { }

    IntegrationTestLogger(IntegrationTestOutputAccessor outputAccessor, LogLevel minimumLevel, IEnumerable<PrefixEnricher> prefixEnrichers)
    {
        _outputAccessor = outputAccessor ?? throw new ArgumentNullException(nameof(outputAccessor));
        _minimumLevel = minimumLevel;
        _prefixEnrichers = prefixEnrichers?.ToList() ?? throw new ArgumentNullException(nameof(prefixEnrichers));
    }

    private void AddPrefixEnricher(PrefixEnricher prefixEnricher)
    {
        ArgumentNullException.ThrowIfNull(prefixEnricher);
        _prefixEnrichers.Add(prefixEnricher);
    }

    public void IncludeThreadId() => AddPrefixEnricher(prefix => $"{prefix} #{System.Environment.CurrentManagedThreadId,3}");

    public void IncludeTimestamps() => AddPrefixEnricher(prefix => $"{prefix} {DateTime.Now: HH:mm:ss.fff}");

    public void IncludePrefix(string prefix)
    {
        AddPrefixEnricher(existingPrefix =>
        {
            if (prefix.Length < 9)
            {
                return $"{existingPrefix} {prefix,10}";
            }

            if (prefix.Length < 19)
            {
                return $"{existingPrefix} {prefix,20}";
            }

            if (prefix.Length < 29)
            {
                return $"{existingPrefix} {prefix,30}";
            }

            return $"{existingPrefix} {prefix}";
        });
    }

    public void SetMinimumLevel(LogLevel minimumLevel) => _minimumLevel = minimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel < _minimumLevel)
        {
            return;
        }

        var trimmedLevel = logLevel.ToString().ToUpper().Substring(0, Math.Min(logLevel.ToString().Length, 5));
        var prefix = _prefixEnrichers.Aggregate(
            $"{nameof(IntegrationTestLogger),20} {trimmedLevel,6}",
            (intermediatePrefix, enrichPrefix) => enrichPrefix(intermediatePrefix));
        var message = formatter(state, exception);
        var line = $"{prefix} : {message}";
        try
        {
            _outputAccessor.Output?.WriteLine(line);
            if (exception != null)
            {
                _outputAccessor.Output?.WriteLine(exception.ToStringDemystified());
            }
        }
        catch (Exception)
        {
            // Ignored, trying to log before or after tests cannot be handled properly
        }
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
}

public sealed class IntegrationTestLogger<T> : IntegrationTestLogger, ILogger<T>
{
    public IntegrationTestLogger(IntegrationTestOutputAccessor outputAccessor) : base(outputAccessor) { }
}
