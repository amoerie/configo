﻿using Configo.Database;
using Configo.Database.Tables;
using Configo.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

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
                    NumberOfVariables = tagVariables.Count()
                })
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
            tagRecord = new TagRecord { Name = tag.Name! };
            dbContext.Tags.Add(tagRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {@Tag}", tagRecord);
            return new TagListModel
            {
                Id = tagRecord.Id,
                Name = tagRecord.Name,
                NumberOfVariables = 0
            };
        }

        tagRecord = await dbContext.Tags
            .AsTracking()
            .SingleAsync(t => t.Id == tag.Id, cancellationToken);
        tagRecord.Name = tag.Name!;
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved {@Tag}", tagRecord);

        return new TagListModel
        {
            Id = tagRecord.Id,
            Name = tagRecord.Name,
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
