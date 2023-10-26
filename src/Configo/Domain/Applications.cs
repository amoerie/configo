using System.ComponentModel.DataAnnotations;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

public sealed record ApplicationListModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}

public sealed class ApplicationEditModel
{
    public int? Id { get; init; }

    [Required] [MaxLength(256)] public string? Name { get; set; }
}

public sealed class ApplicationDeleteModel
{
    [Required] public int? Id { get; set; }
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

    public async Task<List<ApplicationListModel>> GetAllApplicationsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all applications");

        var applications = await dbContext.Applications
            .OrderBy(t => t.Name)
            .Select(t => new ApplicationListModel
            {
                Id = t.Id,
                Name = t.Name,
                UpdatedAtUtc = t.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfApplications} applications", applications.Count);

        return applications;
    }

    public async Task<ApplicationListModel> SaveApplicationAsync(ApplicationEditModel application,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving application {@Application}", application);

        ApplicationRecord applicationRecord;
        if (application.Id == 0)
        {
            applicationRecord = new ApplicationRecord
            {
                Name = application.Name!,
                JsonSchema = "",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Applications.Add(applicationRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@Application}", applicationRecord);
            return new ApplicationListModel
            {
                Id = applicationRecord.Id,
                Name = applicationRecord.Name,
                UpdatedAtUtc = applicationRecord.UpdatedAtUtc,
            };
        }

        applicationRecord = await dbContext.Applications
            .AsTracking()
            .SingleAsync(t => t.Id == application.Id, cancellationToken);
        applicationRecord.Name = application.Name!;
        applicationRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved {@Application}", applicationRecord);

        return new ApplicationListModel
        {
            Id = applicationRecord.Id,
            Name = applicationRecord.Name,
            UpdatedAtUtc = applicationRecord.UpdatedAtUtc,
        };
    }

    public async Task DeleteApplicationAsync(ApplicationDeleteModel application, CancellationToken cancellationToken)
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
