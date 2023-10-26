using System.ComponentModel.DataAnnotations;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

public sealed record TagListModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
    public required int NumberOfVariables { get; init; }
}

public sealed class TagEditModel
{
    public int? Id { get; init; }
    
    [Required]
    [MaxLength(256)]
    public string? Name { get; set; }
}


public sealed class TagDeleteModel
{
    [Required]
    public int? Id { get; set; }
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

    public async Task<List<TagListModel>> GetAllTagsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all tags");

        var tags = await dbContext.Tags
            .GroupJoin(
                dbContext.TagVariables,
                tag => tag.Id,
                tagVariable => tagVariable.TagId,
                (tag, tagVariables) => new TagListModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    UpdatedAtUtc = tag.UpdatedAtUtc,
                    NumberOfVariables = tagVariables.Count()
                })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTags} tags", tags.Count);

        return tags;
    }

    public async Task<TagListModel> SaveTagAsync(TagEditModel tag, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving tag {@Tag}", tag);

        TagRecord tagRecord;
        if (tag.Id == 0)
        {
            tagRecord = new TagRecord { Name = tag.Name!, TagGroupId = 0, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
            dbContext.Tags.Add(tagRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@Tag}", tagRecord);
            return new TagListModel
            {
                Id = tagRecord.Id,
                Name = tagRecord.Name,
                UpdatedAtUtc = tagRecord.UpdatedAtUtc,
                NumberOfVariables = 0,
            };
        }

        tagRecord = await dbContext.Tags
            .AsTracking()
            .SingleAsync(t => t.Id == tag.Id, cancellationToken);
        tagRecord.Name = tag.Name!;
        tagRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved {@Tag}", tagRecord);

        return new TagListModel
        {
            Id = tagRecord.Id,
            Name = tagRecord.Name,
            UpdatedAtUtc = tagRecord.UpdatedAtUtc,
            NumberOfVariables = await dbContext.TagVariables.CountAsync(tv => tv.TagId == tagRecord.Id, cancellationToken)
        };
    }

    public async Task DeleteTagAsync(TagDeleteModel tag, CancellationToken cancellationToken)
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
