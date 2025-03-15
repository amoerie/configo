using System.Security.Claims;
using Configo.Database;
using Configo.Database.Tables;
using KeyedSemaphores;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record UserModel(string Email, string? GivenName, string? FamilyName)
{
    public static UserModel FromRecord(UserRecord record) => new UserModel(record.Email, record.GivenName, record.FamilyName);
}

public class UserManager(IDbContextFactory<ConfigoDbContext> dbContextFactory, ILogger<UserManager> logger)
{
    private readonly KeyedSemaphoresDictionary<string> _emailSemaphores = new KeyedSemaphoresDictionary<string>();

    public async Task<UserModel> GetOrCreateUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var givenName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
        var familyName = principal.FindFirst(ClaimTypes.Surname)?.Value;

        if (email == null)
        {
            throw new ArgumentException("User does not have an email address", nameof(principal));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is not null)
        {
            return UserModel.FromRecord(user);
        }

        // Super simple email based locking to ensure we only try to create the same user once
        using var _ = await _emailSemaphores.LockAsync(email, cancellationToken);

        user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is not null)
        {
            return UserModel.FromRecord(user);
        }

        logger.LogInformation("Creating new user {Email}", email);
        user = new UserRecord
        {
            Email = email,
            GivenName = givenName,
            FamilyName = familyName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return UserModel.FromRecord(user);
    }

}
