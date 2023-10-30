using Configo.Database;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

public class SchemaManager 
{
    private readonly ILogger<SchemaManager> _logger;
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;

    public SchemaManager(
        ILogger<SchemaManager> logger,
        IDbContextFactory<ConfigoDbContext> dbContextFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }
    
    public async Task<string> GetSchemaAsync(string application, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting JSON schema of application with name {Name}", application);

        var schema = await dbContext.Applications
            .Where(a => a.Name == application)
            .Select(a => a.JsonSchema)
            .SingleAsync(cancellationToken);

        _logger.LogInformation("Got JSON schema of application with name {Name}", application);

        return schema;
    }
    
    public async Task SaveSchemaAsync(string application, string schema, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving schema of application {Application}", application);

        await dbContext.Applications
            .Where(a => a.Name == application)
            .ExecuteUpdateAsync(u => u.SetProperty(a => a.JsonSchema, schema), cancellationToken);

        _logger.LogInformation("Saved schema of application {Application}", application);
    }
}
