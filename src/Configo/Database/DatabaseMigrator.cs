using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Configo.Database;

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
        var connectionString = database.GetConnectionString();
        var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

        _logger.LogInformation("Automatically migrating database : {Database} on server {DataSource}",
            connectionStringBuilder.InitialCatalog, connectionStringBuilder.DataSource);

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
