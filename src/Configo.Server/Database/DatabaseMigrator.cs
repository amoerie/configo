using Configo.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Configo.Server.Database;

public class DatabaseMigrator : IHostedService
{
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(IDbContextFactory<ConfigoDbContext> dbContextFactory,
        ILogger<DatabaseMigrator> logger)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var database = dbContext.Database;
        
        if (database.IsSqlServer())
        {
            var connectionString = database.GetConnectionString();
            var builder = new SqlConnectionStringBuilder(connectionString);
            _logger.LogInformation("Automatically migrating SQL server database : {Database} on server {Server}",
                builder.InitialCatalog, builder.DataSource);
        }
        else if (database.IsNpgsql())
        {
            var connectionString = database.GetConnectionString();
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            _logger.LogInformation("Automatically migrating PostgreSQL server database : {Database} on server {Server}:{Port}",
                builder.Database, builder.Host, builder.Port);
        }
        else
        {
            throw new InvalidOperationException("Unsupported database engine: " + database.ProviderName);
        }

        try
        {
            await database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Successfully automatically migrated the database scheme");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Failed to automatically migrate the database scheme");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
