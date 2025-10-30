using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
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

    private PostgreSqlContainer _dbContainer = null!;
    private RedisContainer _redisContainer = null!;
    private Respawner _respawner = null!;
    private TestWebApplicationFactory _testWebApplicationFactory = null!;
    private NpgsqlConnectionStringBuilder _dbConnectionString = null!;
    private ConfigurationOptions _redisConnectionString = null!;
    private readonly IntegrationTestOutputAccessor _outputAccessor = new IntegrationTestOutputAccessor();
    private ILogger<IntegrationTestFixture>? _logger;
    private ConnectionMultiplexer _redisConnection = null!;

    public async Task InitializeAsync()
    {
        // setup database + redis
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("configo")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .Build();
        _redisContainer = new RedisBuilder()
            .Build();
        await Task.WhenAll(_dbContainer.StartAsync(), _redisContainer.StartAsync());
        
        _dbConnectionString = new NpgsqlConnectionStringBuilder(_dbContainer.GetConnectionString()) { IncludeErrorDetail = true };
        _redisConnectionString = ConfigurationOptions.Parse(_redisContainer.GetConnectionString());
        // This is needed to allow FLUSHDB
        _redisConnectionString.AllowAdmin = true;
        _testWebApplicationFactory = new TestWebApplicationFactory(_dbConnectionString,  _redisConnectionString, _outputAccessor);
        
        // By using any of the APIs, we ensure that the server is running
        using var _ = _testWebApplicationFactory.CreateClient();

        _logger = _testWebApplicationFactory.Services.GetRequiredService<ILogger<IntegrationTestFixture>>();
        _logger.LogInformation("Creating database respawn point");

        await using var connection = new NpgsqlConnection(_dbConnectionString.ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude =
            [
                "public"
            ],
            DbAdapter = DbAdapter.Postgres
        });
        
        _redisConnection = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);

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
        await Task.WhenAll(ResetDbAsync(), ResetRedisAsync());
        _outputAccessor.Output = null;
    }

    private async Task ResetDbAsync()
    {
        _logger?.LogInformation("Resetting database");
        await using var connection = new NpgsqlConnection(_dbConnectionString.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    private async Task ResetRedisAsync()
    {
        _logger?.LogInformation("Resetting redis");
        var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
        await server.FlushDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _dbContainer.StopAsync();
            await _testWebApplicationFactory.DisposeAsync();
        }
        finally
        {
            await _dbContainer.DisposeAsync();
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
        _prefixEnrichers = prefixEnrichers.ToList() ?? throw new ArgumentNullException(nameof(prefixEnrichers));
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
