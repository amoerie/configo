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
    public required int TagGroupId { get; init; }

    [Required] [MaxLength(256)] public string? Name { get; set; }
}

public sealed class TagDeleteModel
{
    [Required] public int? Id { get; set; }
}

public sealed record TagDropdownModel
{
    public int Id { get; init; }
    public required string CombinedName { get; init; }
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
            .Select(t => new { t.Id, t.TagGroupId, t.Name })
            .ToListAsync(cancellationToken);
        var tagGroupRecords = await dbContext.TagGroups
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);
            
        var tagGroupRecordsById = tagGroupRecords.ToDictionary(r => r.Id);

        _logger.LogInformation("Got {NumberOfTags} tags", tagRecords.Count);
        
        return tagRecords.Select(tagRecord => new TagDropdownModel
            {
                Id = tagRecord.Id,
                CombinedName = $"{tagGroupRecordsById[tagRecord.TagGroupId].Name}:{tagRecord.Name}"
            })
            .ToList();
    }
    
    public async Task<List<TagListModel>> GetTagsOfGroupAsync(int tagGroupId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting tags of group {GroupId}", tagGroupId);

        var tags = await dbContext.Tags
            .Where(t => t.TagGroupId == tagGroupId)
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

        _logger.LogInformation("Got {NumberOfTags} tags of group {GroupId}", tags.Count, tagGroupId);

        return tags;
    }

    public async Task<TagListModel> SaveTagAsync(TagEditModel tag, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving tag {@Tag}", tag);

        TagRecord tagRecord;
        if (tag.Id == 0)
        {
            if (await dbContext.Tags.AnyAsync(t => t.Name == tag.Name, cancellationToken))
            {
                throw new ArgumentException("Tag name already in use");
            }
            
            tagRecord = new TagRecord
            {
                Name = tag.Name!,
                TagGroupId = tag.TagGroupId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
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
        
        if (await dbContext.Tags.AnyAsync(t => t.Id != tag.Id && t.Name == tag.Name, cancellationToken))
        {
            throw new ArgumentException("Tag name already in use");
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
            NumberOfVariables =
                await dbContext.TagVariables.CountAsync(tv => tv.TagId == tagRecord.Id, cancellationToken)
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
