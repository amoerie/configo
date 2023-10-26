using System.ComponentModel.DataAnnotations;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

public sealed record TagGroupListModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
    public required int NumberOfTags { get; init; }
}

public sealed class TagGroupEditModel
{
    public int? Id { get; init; }

    [Required] [MaxLength(256)] public string? Name { get; set; }
}

public sealed class TagGroupDeleteModel
{
    [Required] public int? Id { get; set; }
}

public sealed class TagGroupManager
{
    private readonly ILogger<TagGroupManager> _logger;
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;

    public TagGroupManager(ILogger<TagGroupManager> logger, IDbContextFactory<ConfigoDbContext> dbContextFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public async Task<List<TagGroupListModel>> GetAllTagGroupsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all tagGroups");

        var tagGroups = await dbContext.TagGroups
            .GroupJoin(
                dbContext.Tags,
                tagGroup => tagGroup.Id,
                tag => tag.TagGroupId,
                (tagGroup, tags) => new TagGroupListModel
                {
                    Id = tagGroup.Id,
                    Name = tagGroup.Name,
                    UpdatedAtUtc = tagGroup.UpdatedAtUtc,
                    NumberOfTags = tags.Count()
                })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfTagGroups} tagGroups", tagGroups.Count);

        return tagGroups;
    }

    public async Task<TagGroupListModel> SaveTagGroupAsync(TagGroupEditModel tagGroup,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving tag group {@TagGroup}", tagGroup);

        TagGroupRecord tagGroupRecord;
        if (tagGroup.Id == 0)
        {
            tagGroupRecord = new TagGroupRecord
            {
                Name = tagGroup.Name!,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.TagGroups.Add(tagGroupRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@TagGroup}", tagGroupRecord);
            return new TagGroupListModel
            {
                Id = tagGroupRecord.Id,
                Name = tagGroupRecord.Name,
                UpdatedAtUtc = tagGroupRecord.UpdatedAtUtc,
                NumberOfTags = 0,
            };
        }

        tagGroupRecord = await dbContext.TagGroups
            .AsTracking()
            .SingleAsync(t => t.Id == tagGroup.Id, cancellationToken);
        tagGroupRecord.Name = tagGroup.Name!;
        tagGroupRecord.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved {@TagGroup}", tagGroupRecord);

        return new TagGroupListModel
        {
            Id = tagGroupRecord.Id,
            Name = tagGroupRecord.Name,
            UpdatedAtUtc = tagGroupRecord.UpdatedAtUtc,
            NumberOfTags = await dbContext.Tags.CountAsync(t => t.TagGroupId == tagGroupRecord.Id,
                cancellationToken)
        };
    }

    public async Task DeleteTagGroupAsync(TagGroupDeleteModel tagGroup, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Deleting tagGroup {@TagGroup}", tagGroup);

        var tagGroupRecord = await dbContext.TagGroups
            .AsTracking()
            .SingleAsync(t => t.Id == tagGroup.Id, cancellationToken);

        dbContext.TagGroups.Remove(tagGroupRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted tagGroup {@TagGroup}", tagGroup);
    }
}
