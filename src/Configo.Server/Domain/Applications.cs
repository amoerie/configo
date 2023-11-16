using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record ApplicationModel
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}

public sealed record ApplicationDropdownModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
}

public sealed class ApplicationManager
{
    private readonly ILogger<ApplicationManager> _logger;
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;

    public ApplicationManager(ILogger<ApplicationManager> logger, IDbContextFactory<ConfigoDbContext> dbContextFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }


    public async Task<List<ApplicationDropdownModel>> GetAllApplicationsForDropdownAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all applications");

        var applications = await dbContext.Applications
            .OrderBy(t => t.Name)
            .Select(t => new ApplicationDropdownModel
            {
                Id = t.Id,
                Name = t.Name,
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfApplications} applications", applications.Count);

        return applications;
    }

    public async Task<List<ApplicationModel>> GetAllApplicationsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all applications");

        var applications = await dbContext.Applications
            .OrderBy(t => t.Name)
            .Select(t => new ApplicationModel
            {
                Id = t.Id,
                Name = t.Name,
                UpdatedAtUtc = t.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfApplications} applications", applications.Count);

        return applications;
    }

    public async Task<ApplicationModel?> GetApplicationByNameAsync(string? name, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting application by name {Name}", name);

        var application = await dbContext.Applications
            .Where(a => a.Name == name)
            .Select(t => new ApplicationModel
            {
                Id = t.Id,
                Name = t.Name,
                UpdatedAtUtc = t.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        _logger.LogInformation("Found {@Application} applications", application);

        return application;
    }

    public async Task SaveApplicationAsync(ApplicationModel model,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving application {@Application}", model);

        ApplicationRecord applicationRecord;
        if (model.Id is 0)
        {
            if (await dbContext.Applications.AnyAsync(t => t.Name == model.Name, cancellationToken))
            {
                throw new ArgumentException("Application name already in use");
            }
            
            applicationRecord = new ApplicationRecord
            {
                Name = model.Name!,
                JsonSchema = "",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Applications.Add(applicationRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@Application}", applicationRecord);
            model.Id = applicationRecord.Id;
            model.Name = applicationRecord.Name;
            model.UpdatedAtUtc = applicationRecord.UpdatedAtUtc;
            return;
        }
        
        if (await dbContext.Applications.AnyAsync(t => t.Id != model.Id && t.Name == model.Name, cancellationToken))
        {
            throw new ArgumentException("Application name already in use");
        }

        applicationRecord = await dbContext.Applications
            .AsTracking()
            .SingleAsync(t => t.Id == model.Id, cancellationToken);
        applicationRecord.Name = model.Name!;
        applicationRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved {@Application}", applicationRecord);

        model.Id = applicationRecord.Id;
        model.Name = applicationRecord.Name;
        model.UpdatedAtUtc = applicationRecord.UpdatedAtUtc;
    }

    public async Task DeleteApplicationAsync(ApplicationModel application, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Deleting application {@Application}", application);

        var applicationRecord = await dbContext.Applications
            .AsTracking()
            .SingleAsync(t => t.Id == application.Id, cancellationToken);

        dbContext.Applications.Remove(applicationRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted application {@Application}", application);
    }
}
