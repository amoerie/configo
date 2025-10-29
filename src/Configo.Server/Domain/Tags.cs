using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record TagListModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required int TagGroupId { get; init; }
    public required string TagGroupName { get; init; }
    public required int TagGroupOrder { get; init; }
    public required int NumberOfVariables { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}

public sealed record TagFormModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required int TagGroupId { get; set; }
}

public sealed record TagDropdownModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required int GroupId { get; init; }
    public required int GroupOrder { get; init; }
    public required string GroupName { get; init; }
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
            .Join(dbContext.TagGroups, tag => tag.TagGroupId, tagGroup => tagGroup.Id, (tag, tagGroup) => new { Tag = tag, TagGroup = tagGroup })
            .OrderBy(t => t.TagGroup.Order).ThenBy(t => t.Tag.Name)
            .Select(t => new TagDropdownModel{ Id = t.Tag.Id, Name = t.Tag.Name, GroupId = t.TagGroup.Id, GroupOrder = t.TagGroup.Order, GroupName = t.TagGroup.Name })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTags} tags", tagRecords.Count);
        
        return tagRecords;
    }
    
    public async Task<List<TagListModel>> GetAllTagsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting tags");

        var tags = await dbContext.Tags
            .Join(dbContext.TagGroups,
                tag => tag.TagGroupId,
                tagGroup => tagGroup.Id, 
                (tag, tagGroup) => new { Tag = tag, TagGroup = tagGroup })
            .GroupJoin(
                dbContext.Variables,
                tagAndTagGroup => tagAndTagGroup.Tag.Id,
                variable => variable.TagId,
                (tagAndTagGroup, variables) => new TagListModel
                {
                    Id = tagAndTagGroup.Tag.Id,
                    Name = tagAndTagGroup.Tag.Name,
                    TagGroupId = tagAndTagGroup.TagGroup.Id,
                    TagGroupName = tagAndTagGroup.TagGroup.Name,
                    TagGroupOrder = tagAndTagGroup.TagGroup.Order,
                    UpdatedAtUtc = tagAndTagGroup.Tag.UpdatedAtUtc,
                    NumberOfVariables = variables.Count()
                })
            .OrderBy(t => t.TagGroupOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTags} tags", tags.Count);

        return tags;
    }

    public async Task SaveTagAsync(TagFormModel formModel, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving tag {@Tag}", formModel);

        if (!await dbContext.TagGroups.AnyAsync(t => t.Id == formModel.TagGroupId, cancellationToken))
        {
            throw new ArgumentException($"Tag group with id {formModel.TagGroupId} does not exist");
        }
        
        TagRecord tagRecord;
        if (formModel.Id is 0)
        {
            if (await dbContext.Tags.AnyAsync(t => t.Name == formModel.Name, cancellationToken))
            {
                throw new ArgumentException("Tag name already in use");
            }
            
            tagRecord = new TagRecord
            {
                Name = formModel.Name,
                TagGroupId = formModel.TagGroupId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Tags.Add(tagRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@Tag}", tagRecord);
            formModel.Id = tagRecord.Id;
            return;
        }
        
        if (await dbContext.Tags.AnyAsync(t => t.Id != formModel.Id && t.Name == formModel.Name, cancellationToken))
        {
            throw new ArgumentException($"Tag name {formModel.Name} already in use by another tag");
        }

        tagRecord = await dbContext.Tags
            .AsTracking()
            .SingleAsync(t => t.Id == formModel.Id, cancellationToken);
        tagRecord.Name = formModel.Name;
        tagRecord.TagGroupId = formModel.TagGroupId;
        tagRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Saved {@Tag}", tagRecord);
    }

    public async Task DeleteTagAsync(TagListModel tag, CancellationToken cancellationToken)
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
