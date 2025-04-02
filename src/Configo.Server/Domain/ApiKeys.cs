using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Configo.Server.Domain;

public sealed record ApiKeyModel
{
    public int Id { get; set; }
    public required int ApplicationId { get; set; }
    public string Key { get; set; } = "";
    public required IEnumerable<int> TagIds { get; set; }
    public required DateTime ActiveSinceUtc { get; set; }
    public required DateTime ActiveUntilUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
  
public sealed record ApiKeyValidationModel
{
    public required int Id { get; set; }
    public required DateTime ActiveSinceUtc { get; init; }
    public required DateTime ActiveUntilUtc { get; init; }
}

public sealed class ApiKeyManager
{
    private readonly ILogger<ApiKeyManager> _logger;
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ApiKeyGenerator _apiKeyGenerator;

    public ApiKeyManager(ILogger<ApiKeyManager> logger,
        IDbContextFactory<ConfigoDbContext> dbContextFactory,
        ApiKeyGenerator apiKeyGenerator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _apiKeyGenerator = apiKeyGenerator ?? throw new ArgumentNullException(nameof(apiKeyGenerator));
    }
    
    public async Task<List<ApiKeyModel>> GetAllApiKeysAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all apiKeys");

        var apiKeys = new List<ApiKeyModel>();
        var apiKeyRecords = await dbContext.ApiKeys.ToListAsync(cancellationToken);

        foreach (var apiKeyRecord in apiKeyRecords)
        {
            var apiKeyTagIds = await dbContext.ApiKeyTags
                .Where(apiKeyTag => apiKeyTag.ApiKeyId == apiKeyRecord.Id)
                .Join(dbContext.Tags, a => a.TagId, t => t.Id, (apiKeyTag, tag) => new { ApiKeyTag = apiKeyTag, Tag = tag })
                .Join(dbContext.TagGroups, a => a.Tag.TagGroupId, t => t.Id, (apiKeyTagAndTag, tagGroup) => new { apiKeyTagAndTag.ApiKeyTag, apiKeyTagAndTag.Tag, TagGroup = tagGroup })
                .OrderBy(result => result.TagGroup.Order)
                .Select(result => result.Tag.Id)
                .ToListAsync(cancellationToken);

            var apiKey = new ApiKeyModel
            {
                Id = apiKeyRecord.Id,
                ApplicationId = apiKeyRecord.ApplicationId,
                Key = apiKeyRecord.Key,
                ActiveSinceUtc = apiKeyRecord.ActiveSinceUtc,
                ActiveUntilUtc = apiKeyRecord.ActiveUntilUtc,
                UpdatedAtUtc = apiKeyRecord.UpdatedAtUtc,
                TagIds = apiKeyTagIds
            };

            apiKeys.Add(apiKey);
        }

        _logger.LogInformation("Got {NumberOfApiKeys} apiKeys", apiKeys.Count);
        
        return apiKeys;
    }
    
    public async Task<ApiKeyValidationModel?> GetApiKeyForValidationAsync(string key, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all apiKeys");

        var apiKeyRecord = await dbContext.ApiKeys
            .Where(a => a.Key == key)
            .SingleOrDefaultAsync(cancellationToken);

        if (apiKeyRecord == null)
        {
            return null;
        }
        
        return new ApiKeyValidationModel
        {
            Id = apiKeyRecord.Id,
            ActiveSinceUtc = apiKeyRecord.ActiveSinceUtc,
            ActiveUntilUtc = apiKeyRecord.ActiveUntilUtc,
        };
    }

    public async Task SaveApiKeyAsync(ApiKeyModel model, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving apiKey {@ApiKey}", model);

        ApiKeyRecord apiKeyRecord;
        if (model.Id is 0)
        {
            apiKeyRecord = new ApiKeyRecord
            {
                ApplicationId = model.ApplicationId,
                Key = _apiKeyGenerator.Generate(64),
                ActiveSinceUtc = model.ActiveSinceUtc,
                ActiveUntilUtc = model.ActiveUntilUtc,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            dbContext.ApiKeys.Add(apiKeyRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            apiKeyRecord = await dbContext.ApiKeys
                .AsTracking()
                .SingleAsync(t => t.Id == model.Id, cancellationToken);
            apiKeyRecord.ApplicationId = model.ApplicationId;
            apiKeyRecord.ActiveSinceUtc = model.ActiveSinceUtc;
            apiKeyRecord.ActiveUntilUtc = model.ActiveUntilUtc;
            apiKeyRecord.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await dbContext.ApiKeyTags
                .Where(a => a.ApiKeyId == apiKeyRecord.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        _logger.LogInformation("Saved {@ApiKey}", apiKeyRecord);

        foreach (var tagId in model.TagIds)
        {
            var apiKeyTagRecord = new ApiKeyTagRecord
            {
                ApiKeyId = apiKeyRecord.Id,
                TagId = tagId,
            };
            dbContext.ApiKeyTags.Add(apiKeyTagRecord);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        
        var apiKeyTagIds = await dbContext.ApiKeyTags
            .Where(apiKeyTag => apiKeyTag.ApiKeyId == apiKeyRecord.Id)
            .Join(dbContext.Tags, a => a.TagId, t => t.Id, (apiKeyTag, tag) => new { ApiKeyTag = apiKeyTag, Tag = tag })
            .Join(dbContext.TagGroups, a => a.Tag.TagGroupId, t => t.Id, (apiKeyTagAndTag, tagGroup) => new { apiKeyTagAndTag.ApiKeyTag, apiKeyTagAndTag.Tag, TagGroup = tagGroup })
            .OrderBy(result => result.TagGroup.Order)
            .Select(result => result.Tag.Id)
            .ToListAsync(cancellationToken);

        model.Id = apiKeyRecord.Id;
        model.Key = apiKeyRecord.Key;
        model.ActiveSinceUtc = apiKeyRecord.ActiveSinceUtc;
        model.ActiveUntilUtc = apiKeyRecord.ActiveUntilUtc;
        model.UpdatedAtUtc = apiKeyRecord.UpdatedAtUtc;
        model.ApplicationId = apiKeyRecord.ApplicationId;
        model.TagIds = apiKeyTagIds;
    }

    public async Task DeleteApiKeyAsync(ApiKeyModel apiKey, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Deleting apiKey {@ApiKey}", apiKey);

        var apiKeyRecord = await dbContext.ApiKeys
            .AsTracking()
            .SingleAsync(t => t.Id == apiKey.Id, cancellationToken);

        dbContext.ApiKeys.Remove(apiKeyRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted apiKey {@ApiKey}", apiKey);
    }
}

/// <summary>
/// API keys contains all characters from A-Z and digits from 0-9 with the following exceptions:
///     - The digit 0 and the letter O are excluded to avoid confusion
///     - The digit 1 and the letter L are excluded to avoid confusion
///     - The digit 8 is excluded to avoid confusion with the letter B, but B is not excluded
/// </summary>
public sealed class ApiKeyGenerator
{
    public const string AllowedCharacters = "ABCDEFGHJKMNPQRSTUVWXYZ2345679";
    public const string ForbiddenCharacters = "ILO018";

    public string Generate(int length)
    {
        if (length < 1)
        {
            throw new ArgumentException("Length must be greater than zero", nameof(length));
        }

        var output = new StringBuilder(length);
        var rnd = Random.Shared;
        for (var i = 0; i < length; i++)
        {
            var next = rnd.Next(0, AllowedCharacters.Length);
            output.Append(AllowedCharacters[next]);
        }
        return output.ToString();
    }
}

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IOptionsMonitor<ApiKeyAuthenticationOptions> _options;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ApiKeyManager _apiKeyManager;
    
    public const string AuthenticationScheme = "Configo-Api-Key";
    public const string ApiKeyIdClaim = "api_key_id";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<ApiKeyAuthenticationHandler> logger,
        UrlEncoder encoder,
        TimeProvider timeProvider, ApiKeyManager apiKeyManager) : base(options, loggerFactory, encoder)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers["Authorization"].ToString();

        // If the header is empty or does not start with "Bearer ", return no result
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        // Extract the API key from the header
        var key = authorizationHeader.Substring("Bearer ".Length).Trim();
        var keyForLogging = key.Length > 10
            ? key.Substring(0, 5) + "..." + key.Substring(key.Length - 5)
            : key;

        // Validate the API key
        var apiKey = await _apiKeyManager.GetApiKeyForValidationAsync(key, CancellationToken.None);
        if (apiKey == null)
        {
            _logger.LogWarning("Authentication failed - Invalid API key because key not found: {Key}", keyForLogging);
            return AuthenticateResult.NoResult();
        }

        var utcNow = _timeProvider.GetUtcNow().DateTime;
        if (apiKey.ActiveSinceUtc.Subtract(_options.CurrentValue.ClockSkew) > utcNow)
        {
            _logger.LogWarning("Authentication failed - API key not yet active: {Key}", keyForLogging);
            return AuthenticateResult.NoResult();
        }

        if (apiKey.ActiveUntilUtc.Add(_options.CurrentValue.ClockSkew) < utcNow)
        {
            _logger.LogWarning("Authentication failed - API key expired: {Key}", keyForLogging);
            return AuthenticateResult.NoResult();
        }

        _logger.LogInformation("Valid API key: {Key}", keyForLogging);
        
        var identity = new ClaimsIdentity(Scheme.Name);
        
        identity.AddClaim(new Claim(ApiKeyIdClaim, apiKey.Id.ToString(CultureInfo.InvariantCulture)));

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
