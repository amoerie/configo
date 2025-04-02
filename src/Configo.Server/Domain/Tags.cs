using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record TagModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required int TagGroupId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int NumberOfVariables { get; set; }
}

public sealed record TagDropdownModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

public sealed class TagManager
{
    private readonly ILogger<TagManager> _logger;
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;

    public TagManager(ILogger<TagManager> logger, IDbContextFactory<ConfigoDbContext> dbContextFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public async Task<List<TagDropdownModel>> GetAllTagsForDropdownAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all tags");

        var tagRecords = await dbContext.Tags
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTags} tags", tagRecords.Count);
        
        return tagRecords.Select(tagRecord => new TagDropdownModel
            {
                Id = tagRecord.Id,
                Name = tagRecord.Name
            })
            .ToList();
    }
    
    public async Task<List<TagModel>> GetAllTagsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting tags");

        var tags = await dbContext.Tags
            .GroupJoin(
                dbContext.Variables,
                tag => tag.Id,
                variable => variable.TagId,
                (tag, variables) => new TagModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    TagGroupId = tag.TagGroupId,
                    UpdatedAtUtc = tag.UpdatedAtUtc,
                    NumberOfVariables = variables.Count()
                })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTags} tags", tags.Count);

        return tags;
    }

    public async Task SaveTagAsync(TagModel model, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving tag {@Tag}", model);

        if (!await dbContext.TagGroups.AnyAsync(t => t.Id == model.TagGroupId, cancellationToken))
        {
            throw new ArgumentException($"Tag group with id {model.TagGroupId} does not exist");
        }
        
        TagRecord tagRecord;
        if (model.Id is 0)
        {
            if (await dbContext.Tags.AnyAsync(t => t.Name == model.Name, cancellationToken))
            {
                throw new ArgumentException("Tag name already in use");
            }
            
            tagRecord = new TagRecord
            {
                Name = model.Name,
                TagGroupId = model.TagGroupId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Tags.Add(tagRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@Tag}", tagRecord);
            model.Id = tagRecord.Id;
            model.Name = tagRecord.Name;
            model.UpdatedAtUtc = tagRecord.UpdatedAtUtc;
            model.NumberOfVariables = 0;
            return;
        }
        
        if (await dbContext.Tags.AnyAsync(t => t.Id != model.Id && t.Name == model.Name, cancellationToken))
        {
            throw new ArgumentException("Tag name already in use");
        }

        tagRecord = await dbContext.Tags
            .AsTracking()
            .SingleAsync(t => t.Id == model.Id, cancellationToken);
        tagRecord.Name = model.Name!;
        tagRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved {@Tag}", tagRecord);

        model.Id = tagRecord.Id;
        model.Name = tagRecord.Name;
        model.UpdatedAtUtc = tagRecord.UpdatedAtUtc;
        model.NumberOfVariables = await dbContext.Variables.CountAsync(tv => tv.TagId == tagRecord.Id, cancellationToken);
    }

    public async Task DeleteTagAsync(TagModel tag, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Deleting tag {@Tag}", tag);

        var tagRecord = await dbContext.Tags
            .AsTracking()
            .SingleAsync(t => t.Id == tag.Id, cancellationToken);

        dbContext.Tags.Remove(tagRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted tag {@Tag}", tag);
    }
}
