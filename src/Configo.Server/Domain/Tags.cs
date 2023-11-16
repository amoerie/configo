using Configo.Database;
using Configo.Database.Tables;
using Configo.Server.Blazor;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record TagModel
{
    public int Id { get; set; }
    
    public required int GroupId { get; set; }
    
    public required TagGroupIcon GroupIcon { get; set; }
    public required string Name { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int NumberOfVariables { get; set; }
}

public sealed record TagDropdownModel
{
    public required int Id { get; init; }
    public required int GroupId { get; init; }
    
    public required TagGroupIcon GroupIcon { get; init; }
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
            .Select(t => new { t.Id, t.TagGroupId, t.Name })
            .ToListAsync(cancellationToken);
        var tagGroupRecords = await dbContext.TagGroups
            .Select(t => new { t.Id, t.Name, t.Icon })
            .ToListAsync(cancellationToken);
            
        var tagGroupRecordsById = tagGroupRecords.ToDictionary(r => r.Id);

        _logger.LogInformation("Got {NumberOfTags} tags", tagRecords.Count);
        
        return tagRecords.Select(tagRecord =>
            {
                var group = tagGroupRecordsById[tagRecord.TagGroupId];
                return new TagDropdownModel
                {
                    Id = tagRecord.Id,
                    GroupIcon = TagGroupIcon.GetByName(group.Icon),
                    GroupId = tagRecord.TagGroupId,
                    Name = tagRecord.Name
                };
            })
            .ToList();
    }
    
    public async Task<List<TagModel>> GetTagsOfGroupAsync(int tagGroupId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting tags of group {GroupId}", tagGroupId);

        var group = await dbContext.TagGroups.SingleAsync(g => g.Id == tagGroupId, cancellationToken);

        var tags = await dbContext.Tags
            .Where(t => t.TagGroupId == tagGroupId)
            .GroupJoin(
                dbContext.TagVariables,
                tag => tag.Id,
                tagVariable => tagVariable.TagId,
                (tag, tagVariables) => new TagModel
                {
                    Id = tag.Id,
                    GroupId = group.Id,
                    GroupIcon = TagGroupIcon.GetByName(group.Icon),
                    Name = tag.Name,
                    UpdatedAtUtc = tag.UpdatedAtUtc,
                    NumberOfVariables = tagVariables.Count()
                })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTags} tags of group {GroupId}", tags.Count, tagGroupId);

        return tags;
    }

    public async Task SaveTagAsync(TagModel model, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving tag {@Tag}", model);

        var group = await dbContext.TagGroups.SingleAsync(g => g.Id == model.GroupId, cancellationToken);

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
                TagGroupId = model.GroupId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Tags.Add(tagRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@Tag}", tagRecord);
            model.Id = tagRecord.Id;
            model.Name = tagRecord.Name;
            model.GroupId = tagRecord.TagGroupId;
            model.GroupIcon = TagGroupIcon.GetByName(group.Icon);
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
        model.GroupId = tagRecord.TagGroupId;
        model.GroupIcon = TagGroupIcon.GetByName(group.Icon);
        model.UpdatedAtUtc = tagRecord.UpdatedAtUtc;
        model.NumberOfVariables = await dbContext.TagVariables.CountAsync(tv => tv.TagId == tagRecord.Id, cancellationToken);
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
