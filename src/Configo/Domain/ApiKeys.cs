using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Configo.Domain;

public sealed record ApiKeyListModel
{
    public required int Id { get; init; }
    public required int ApplicationId { get; init; }
    public required string Key { get; init; }
    public required List<int> TagIds { get; init; }
    public required DateTime ActiveSinceUtc { get; init; }
    public required DateTime ActiveUntilUtc { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}

public sealed record ApiKeyEditModel
{
    public int? Id { get; init; }
    
    [Required]
    public required int ApplicationId { get; set; }
    
    [Required]
    public required List<int> TagIds { get; set; }
    
    [Required]
    public required DateTime ActiveSinceUtc { get; set; }
    
    [Required]
    public required DateTime ActiveUntilUtc { get; set; }
}

public sealed record ApiKeyDeleteModel
{
    [Required] public int? Id { get; set; }
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
    
    public async Task<List<ApiKeyListModel>> GetAllApiKeysAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting all apiKeys");

        var apiKeys = new List<ApiKeyListModel>();
        var apiKeyRecords = await dbContext.ApiKeys.ToListAsync(cancellationToken);

        foreach (var apiKeyRecord in apiKeyRecords)
        {
            var apiKeyTagIds = await dbContext.ApiKeyTags
                .Where(apiKeyTag => apiKeyTag.ApiKeyId == apiKeyRecord.Id)
                .Select(apiKeyTag => apiKeyTag.TagId)
                .ToListAsync(cancellationToken);

            var apiKey = new ApiKeyListModel
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

    public async Task<ApiKeyListModel> SaveApiKeyAsync(ApiKeyEditModel apiKey, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving apiKey {@ApiKey}", apiKey);

        ApiKeyRecord apiKeyRecord;
        if (apiKey.Id is null or 0)
        {
            apiKeyRecord = new ApiKeyRecord
            {
                ApplicationId = apiKey.ApplicationId,
                Key = _apiKeyGenerator.Generate(64),
                ActiveSinceUtc = apiKey.ActiveSinceUtc,
                ActiveUntilUtc = apiKey.ActiveUntilUtc,
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
                .SingleAsync(t => t.Id == apiKey.Id, cancellationToken);
            apiKeyRecord.ApplicationId = apiKey.ApplicationId;
            apiKeyRecord.ActiveSinceUtc = apiKey.ActiveSinceUtc;
            apiKeyRecord.ActiveUntilUtc = apiKey.ActiveUntilUtc;
            apiKeyRecord.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await dbContext.ApiKeyTags
                .Where(a => a.ApiKeyId == apiKeyRecord.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        _logger.LogInformation("Saved {@ApiKey}", apiKeyRecord);

        foreach (var tagId in apiKey.TagIds)
        {
            var apiKeyTagRecord = new ApiKeyTagRecord
            {
                ApiKeyId = apiKeyRecord.Id,
                TagId = tagId
            };
            dbContext.ApiKeyTags.Add(apiKeyTagRecord);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ApiKeyListModel
        {
            Id = apiKeyRecord.Id,
            Key = apiKeyRecord.Key,
            ActiveSinceUtc = apiKeyRecord.ActiveSinceUtc,
            ActiveUntilUtc = apiKeyRecord.ActiveUntilUtc,
            UpdatedAtUtc = apiKeyRecord.UpdatedAtUtc,
            ApplicationId = apiKeyRecord.ApplicationId,
            TagIds = apiKey.TagIds.ToList()
        };
    }

    public async Task DeleteApiKeyAsync(ApiKeyDeleteModel apiKey, CancellationToken cancellationToken)
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
    
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
    private readonly ISystemClock _clock;
    private readonly ApiKeyManager _apiKeyManager;
    
    public const string AuthenticationScheme = "Configo-Api-Key";
    public const string ApiKeyIdClaim = "api_key_id";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<ApiKeyAuthenticationHandler> logger,
        UrlEncoder encoder,
        ISystemClock clock, ApiKeyManager apiKeyManager) : base(options, loggerFactory, encoder, clock)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
    }

    // Override the HandleAuthenticateAsync method to implement the custom logic
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get the authorization header from the request
        var authorizationHeader = Request.Headers["Authorization"].ToString();

        // If the header is empty or does not start with "Bearer ", return no result
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        // Extract the API key from the header
        var key = authorizationHeader.Substring("Bearer ".Length).Trim();

        // Validate the API key
        var apiKey = await _apiKeyManager.GetApiKeyForValidationAsync(key, CancellationToken.None);
        if (apiKey == null)
        {
            _logger.LogWarning("Authentication failed - Invalid API key because key not found: {Key}", key);
            return AuthenticateResult.NoResult();
        }

        var utcNow = _clock.UtcNow.DateTime;
        if (apiKey.ActiveSinceUtc > utcNow)
        {
            _logger.LogWarning("Authentication failed - API key not yet active: {Key}", key);
            return AuthenticateResult.NoResult();
        }

        if (apiKey.ActiveUntilUtc < utcNow)
        {
            _logger.LogWarning("Authentication failed - API key expired: {Key}", key);
            return AuthenticateResult.NoResult();
        }

        _logger.LogInformation("Valid API key: {Key}", key);
        
        // Create a claim identity with a dummy name claim
        var identity = new ClaimsIdentity(Scheme.Name);
        
        identity.AddClaim(new Claim(ApiKeyIdClaim, apiKey.Id.ToString(CultureInfo.InvariantCulture)));

        // Create a ticket with the identity and the scheme
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        // Return a success result with the ticket
        return AuthenticateResult.Success(ticket);
    }
}
