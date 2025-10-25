using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record TagGroupModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Order { get; init; }
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

        var tagGroups = await dbContext.TagGroups
            .OrderBy(tagGroupRecord => tagGroupRecord.Order)
            .Select(tagGroupRecord => new TagGroupDropdownModel
            {
                Id = tagGroupRecord.Id,
                Name = tagGroupRecord.Name
            })
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Got {NumberOfTagGroups} tag groups", tagGroups.Count);

        return tagGroups;
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
                    Order = tagGroup.Order,
                    UpdatedAtUtc = tagGroup.UpdatedAtUtc,
                    NumberOfTags = tags.Count()
                })
            .OrderBy(t => t.Order)
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
            var existing = await dbContext.TagGroups.FirstOrDefaultAsync(t => t.Name == model.Name, cancellationToken);
            if (existing is not null)
            {
                 throw new ArgumentException($"Tag group name {model.Name} already in use by group {existing.Id}");
            }

            var maxOrder = await dbContext.TagGroups.Select(o => (int?) o.Order).MaxAsync(cancellationToken);
            tagGroupRecord = new TagGroupRecord
            {
                Name = model.Name,
                Order = (maxOrder + 1) ?? 0,
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

        {
            var existing = await dbContext.TagGroups.FirstOrDefaultAsync(t => t.Id != model.Id && t.Name == model.Name, cancellationToken);
            if (existing is not null)
            {
                throw new ArgumentException($"Tag group name {model.Name} already in use by group {existing.Id}");
            }
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
        ArgumentOutOfRangeException.ThrowIfZero(tagGroup.Id);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        logger.LogDebug("Deleting tag group {@TagGroup}", tagGroup);

        var tagGroupRecord = await dbContext.TagGroups
            .AsTracking()
            .SingleAsync(t => t.Id == tagGroup.Id, cancellationToken);

        dbContext.TagGroups.Remove(tagGroupRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted tag group {@TagGroup}", tagGroup);
    }

    public async Task ChangeOrderAsync(int tagGroupId, int newOrder, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(tagGroupId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        logger.LogDebug("Changing order of tag group {TagGroupId} to {NewOrder}", tagGroupId, newOrder);

        var allTagGroups = await dbContext.TagGroups
            .AsTracking()
            .OrderBy(t => t.Order)
            .ToListAsync(cancellationToken);

        var oldIndex = allTagGroups.FindIndex(tagGroupRecord => tagGroupRecord.Id == tagGroupId);
        if (oldIndex == -1)
        {
            return;
        }
        
        var newIndex = Math.Clamp(newOrder, 0, allTagGroups.Count - 1);
        if (oldIndex == newIndex)
        {
            return;
        }
        
        // To avoid intermediate order values that would conflict with the unique index, assign negative values
        for (int index = 0; index < allTagGroups.Count; index++)
        {
            TagGroupRecord tagGroupRecord = allTagGroups[index];
            tagGroupRecord.Order = -index;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        // Now update the orders
        var temp = allTagGroups[oldIndex];
        allTagGroups.RemoveAt(oldIndex);
        allTagGroups.Insert(newIndex, temp);

        for (int index = 0; index < allTagGroups.Count; index++)
        {
            TagGroupRecord tagGroupRecord = allTagGroups[index];
            tagGroupRecord.Order = index;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reordered tag groups");
    }
}
