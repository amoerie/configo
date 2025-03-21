﻿using Configo.Database;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

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
    
    public async Task<string> GetSchemaAsync(int? applicationId, CancellationToken cancellationToken)
    {
        if (applicationId is null)
        {
            return "{}";
        }
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting JSON schema of application {ApplicationId}", applicationId);

        var schema = await dbContext.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => a.JsonSchema)
            .SingleAsync(cancellationToken);

        _logger.LogInformation("Got JSON schema of application {ApplicationId}", applicationId);

        return schema;
    }
    
    public async Task<string> GetSchemaAsync(List<int> applicationIds, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (applicationIds.Count == 0)
        {
            return "{}";
        }

        if (applicationIds.Count == 1)
        {
            return await GetSchemaAsync(applicationIds[0], cancellationToken);
        }
        
        // TODO merge schemas
        return await GetSchemaAsync(applicationIds[0], cancellationToken);
    }
    
    public async Task SaveSchemaAsync(int applicationId, string schema, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving schema of application {ApplicationId}", applicationId);

        await dbContext.Applications
            .Where(a => a.Id == applicationId)
            .ExecuteUpdateAsync(u => u.SetProperty(a => a.JsonSchema, schema), cancellationToken);

        _logger.LogInformation("Saved schema of application {ApplicationId}", applicationId);
    }
}
