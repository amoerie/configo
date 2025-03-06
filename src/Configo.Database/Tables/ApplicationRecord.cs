using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record ApplicationRecord
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string JsonSchema { get; set; }
    
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}

public class ApplicationRecordConfigurator: IEntityTypeConfiguration<ApplicationRecord>
{
    public void Configure(EntityTypeBuilder<ApplicationRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(256);
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.CreatedAtUtc).HasDefaultValue(DateTime.UnixEpoch);
        builder.Property(r => r.UpdatedAtUtc).HasDefaultValue(DateTime.UnixEpoch);
    }
}
