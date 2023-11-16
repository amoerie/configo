using Configo.Database;
using Configo.Database.Tables;
using Configo.Server.Blazor;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record TagGroupModel
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    
    public required TagGroupIcon GroupIcon { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
    public required int NumberOfTags { get; set; }
}

public sealed class TagGroupManager
{
    private readonly ILogger<TagGroupManager> _logger;
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly List<Func<CancellationToken, Task>> _changeListeners = new List<Func<CancellationToken, Task>>();

    public TagGroupManager(ILogger<TagGroupManager> logger, IDbContextFactory<ConfigoDbContext> dbContextFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public void Subscribe(Func<CancellationToken, Task> listener)
    {
        lock (_changeListeners)
        {
            _changeListeners.Add(listener);
        }
    }

    public void Unsubscribe(Func<CancellationToken, Task> listener)
    {
        lock (_changeListeners)
        {
            _changeListeners.Remove(listener);
        }
    }

    private async Task NotifyListenersAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task>[] listeners;
        lock (_changeListeners)
        {
            listeners = _changeListeners.ToArray();
        }

        foreach (var listener in listeners)
        {
            await listener(cancellationToken);
        }
    }

    public async Task<TagGroupModel> GetTagGroupAsync(string group, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting tag group with name {Name}", group);

        var tagGroups = await dbContext.TagGroups
            .GroupJoin(
                dbContext.Tags,
                tagGroup => tagGroup.Id,
                tag => tag.TagGroupId,
                (tagGroup, tags) => new TagGroupModel
                {
                    Id = tagGroup.Id,
                    Name = tagGroup.Name,
                    GroupIcon = TagGroupIcon.GetByName(tagGroup.Icon),
                    UpdatedAtUtc = tagGroup.UpdatedAtUtc,
                    NumberOfTags = tags.Count()
                })
            .SingleAsync(t => t.Name == group, cancellationToken);

        _logger.LogInformation("Got tag group with name {Name}", group);

        return tagGroups;
    }

    public async Task<List<TagGroupModel>> GetAllTagGroupsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all tag groups");

        var tagGroups = await dbContext.TagGroups
            .GroupJoin(
                dbContext.Tags,
                tagGroup => tagGroup.Id,
                tag => tag.TagGroupId,
                (tagGroup, tags) => new TagGroupModel
                {
                    Id = tagGroup.Id,
                    Name = tagGroup.Name,
                    GroupIcon = TagGroupIcon.GetByName(tagGroup.Icon),
                    UpdatedAtUtc = tagGroup.UpdatedAtUtc,
                    NumberOfTags = tags.Count()
                })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTagGroups} tag groups", tagGroups.Count);

        return tagGroups;
    }

    public async Task SaveTagGroupAsync(TagGroupModel tagGroup,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    
        _logger.LogDebug("Saving tag group {@TagGroup}", tagGroup);
    
        TagGroupRecord tagGroupRecord;
        if (tagGroup.Id is 0)
        {
            if (await dbContext.TagGroups.AnyAsync(t => t.Name == tagGroup.Name, cancellationToken))
            {
                throw new ArgumentException("Tag group name already in use");
            }
            
            tagGroupRecord = new TagGroupRecord
            {
                Name = tagGroup.Name!,
                Icon = tagGroup.GroupIcon.Name,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.TagGroups.Add(tagGroupRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            await NotifyListenersAsync(cancellationToken);
            _logger.LogInformation("Saved {@TagGroup}", tagGroupRecord);
            tagGroup.Id = tagGroupRecord.Id;
            tagGroup.Name = tagGroupRecord.Name;
            tagGroup.GroupIcon = TagGroupIcon.GetByName(tagGroupRecord.Icon);
            tagGroup.UpdatedAtUtc = tagGroupRecord.UpdatedAtUtc;
            tagGroup.NumberOfTags = 0;
        }
    
        if (await dbContext.TagGroups.AnyAsync(t => t.Id != tagGroup.Id && t.Name == tagGroup.Name, cancellationToken))
        {
            throw new ArgumentException("Tag group name already in use");
        }
    
        tagGroupRecord = await dbContext.TagGroups
            .AsTracking()
            .SingleAsync(t => t.Id == tagGroup.Id, cancellationToken);
        tagGroupRecord.Name = tagGroup.Name!;
        tagGroupRecord.Icon = tagGroup.GroupIcon.Name;
        tagGroupRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyListenersAsync(cancellationToken);
        _logger.LogInformation("Saved {@TagGroup}", tagGroupRecord);
    
        tagGroup.Id = tagGroupRecord.Id;
        tagGroup.Name = tagGroupRecord.Name;
        tagGroup.GroupIcon = TagGroupIcon.GetByName(tagGroupRecord.Icon);
        tagGroup.UpdatedAtUtc = tagGroupRecord.UpdatedAtUtc;
        tagGroup.NumberOfTags = await dbContext.Tags.CountAsync(t => t.TagGroupId == tagGroupRecord.Id, cancellationToken);
    }

    public async Task DeleteTagGroupAsync(TagGroupModel tagGroup, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Deleting tag group {@TagGroup}", tagGroup);

        var tagGroupRecord = await dbContext.TagGroups
            .AsTracking()
            .SingleAsync(t => t.Id == tagGroup.Id, cancellationToken);

        dbContext.TagGroups.Remove(tagGroupRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyListenersAsync(cancellationToken);
        _logger.LogInformation("Deleted tag group {@TagGroup}", tagGroup);
    }
}
