using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record ApiKeyRecord
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public required string Key { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
    public required DateTime ActiveSinceUtc { get; set; }
    public required DateTime ActiveUntilUtc { get; set; }
}

public sealed class ApiKeyRecordConfiguration : IEntityTypeConfiguration<ApiKeyRecord>
{
    public void Configure(EntityTypeBuilder<ApiKeyRecord> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasOne<ApplicationRecord>()
            .WithMany()
            .HasForeignKey(a => a.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(r => r.Key).HasMaxLength(64);
        builder.Property(r => r.CreatedAtUtc).HasDefaultValue(DateTime.UnixEpoch);
        builder.Property(r => r.UpdatedAtUtc).HasDefaultValue(DateTime.UnixEpoch);
        builder.Property(r => r.ActiveSinceUtc).HasDefaultValue(DateTime.UnixEpoch);
        builder.Property(r => r.ActiveUntilUtc).HasDefaultValue(DateTime.UnixEpoch);
    }
}
