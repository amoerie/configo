using System.Text.Json;
using System.Text.Json.Nodes;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

public sealed record VariableForConfigModel
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required VariableValueType ValueType { get; set; }
}

public sealed class VariableManager
{
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ILogger<VariableManager> _logger;

    public VariableManager(IDbContextFactory<ConfigoDbContext> dbContextFactory, ILogger<VariableManager> logger)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<VariableForConfigModel>> GetConfigAsync(int apiKeyId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting config for API key {ApiKeyId}", apiKeyId);

        // Get tags of API key
        var tags = await dbContext.Tags
            .GroupJoin(dbContext.ApiKeyTags.Where(apiKeyTag => apiKeyTag.ApiKeyId == apiKeyId),
                t => t.Id,
                a => a.TagId,
                (tag, _) => tag)
            .ToListAsync(cancellationToken);

        var tagIds = tags.Select(t => t.Id).ToList();

        // Get variables that are linked to ALL tags
        var variables = await dbContext.TagVariables
            .Where(tv => tagIds.Contains(tv.TagId))
            .GroupBy(tv => tv.VariableId)
            .Where(g => g.Count() == tags.Count)
            .Select(tv => tv.Key)
            .Join(dbContext.Variables,
                variableId => variableId,
                variable => variable.Id,
                (variableId, variable) => variable)
            .Select(variable => new VariableForConfigModel
            {
                Key = variable.Key,
                Value = variable.Value,
                ValueType = variable.ValueType
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfVariables} variables for API key {ApiKeyId}", apiKeyId);

        return variables;
    }
}

public class VariablesDeserializer
{
    public JsonObject DeserializeVariables(List<VariableForConfigModel> variables)
    {
        var root = new JsonObject();
        foreach (var variable in variables)
        {
            var key = variable.Key;
            var value = variable.Value;
            var valueType = variable.ValueType;

            var path = key.Split(":");
            var parent = root;

            // TODO dynamically build JSON object that represents the variable structure
        }
    }
}
