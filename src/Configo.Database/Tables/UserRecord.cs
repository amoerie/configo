using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record UserRecord
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string? GivenName { get; set; }
    public required string? FamilyName { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}

public class UserRecordConfigurator: IEntityTypeConfiguration<UserRecord>
{
    public void Configure(EntityTypeBuilder<UserRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Email).HasMaxLength(256);
        builder.Property(r => r.GivenName).HasMaxLength(256);
        builder.Property(r => r.FamilyName).HasMaxLength(256);
        builder.HasIndex(r => r.Email).IsUnique();
    }
}
