using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record TagGroupModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int NumberOfTags { get; set; }
}

public sealed record TagGroupDropdownModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

public sealed class TagGroupManager(ILogger<TagGroupManager> logger, IDbContextFactory<ConfigoDbContext> dbContextFactory)
{
    public async Task<List<TagGroupDropdownModel>> GetAllTagGroupsForDropdownAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        logger.LogDebug("Getting all tag groups");

        var tagGroupRecords = await dbContext.TagGroups
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Got {NumberOfTagGroups} tag groups", tagGroupRecords.Count);
        
        return tagGroupRecords.Select(tagGroupRecord => new TagGroupDropdownModel
            {
                Id = tagGroupRecord.Id,
                Name = tagGroupRecord.Name
            })
            .ToList();
    }
    
    public async Task<List<TagGroupModel>> GetAllTagGroupsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        logger.LogDebug("Getting all tag groups");

        var tagGroups = await dbContext.TagGroups
            .GroupJoin(
                dbContext.Tags,
                tagGroup => tagGroup.Id,
                tag => tag.TagGroupId,
                (tagGroup, tags) => new TagGroupModel
                {
                    Id = tagGroup.Id,
                    Name = tagGroup.Name,
                    UpdatedAtUtc = tagGroup.UpdatedAtUtc,
                    NumberOfTags = tags.Count()
                })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Got {NumberOfTagGroups} tag groups", tagGroups.Count);

        return tagGroups;
    }

    public async Task SaveTagGroupAsync(TagGroupModel model, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        logger.LogDebug("Saving tag group {@TagGroup}", model);

        TagGroupRecord tagGroupRecord;
        if (model.Id is 0)
        {
            if (await dbContext.TagGroups.AnyAsync(t => t.Name == model.Name, cancellationToken))
            {
                throw new ArgumentException("Tag group name already in use");
            }
            
            tagGroupRecord = new TagGroupRecord
            {
                Name = model.Name,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.TagGroups.Add(tagGroupRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Saved {@TagGroup}", tagGroupRecord);
            model.Id = tagGroupRecord.Id;
            model.Name = tagGroupRecord.Name;
            model.UpdatedAtUtc = tagGroupRecord.UpdatedAtUtc;
            model.NumberOfTags = 0;
            return;
        }
        
        if (await dbContext.TagGroups.AnyAsync(t => t.Id != model.Id && t.Name == model.Name, cancellationToken))
        {
            throw new ArgumentException("TagGroup name already in use");
        }

        tagGroupRecord = await dbContext.TagGroups
            .AsTracking()
            .SingleAsync(t => t.Id == model.Id, cancellationToken);
        tagGroupRecord.Name = model.Name!;
        tagGroupRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Saved {@TagGroup}", tagGroupRecord);

        model.Id = tagGroupRecord.Id;
        model.Name = tagGroupRecord.Name;
        model.UpdatedAtUtc = tagGroupRecord.UpdatedAtUtc;
        model.NumberOfTags = await dbContext.Tags.CountAsync(tv => tv.TagGroupId == tagGroupRecord.Id, cancellationToken);
    }

    public async Task DeleteTagGroupAsync(TagGroupModel tagGroup, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        logger.LogDebug("Deleting tag group {@TagGroup}", tagGroup);

        var tagGroupRecord = await dbContext.TagGroups
            .AsTracking()
            .SingleAsync(t => t.Id == tagGroup.Id, cancellationToken);

        dbContext.TagGroups.Remove(tagGroupRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted tag group {@TagGroup}", tagGroup);
    }
}
