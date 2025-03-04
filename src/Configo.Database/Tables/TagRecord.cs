using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record TagRecord
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}

public class TagRecordConfigurator: IEntityTypeConfiguration<TagRecord>
{
    public void Configure(EntityTypeBuilder<TagRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(256);
        builder.HasMany<VariableRecord>()
            .WithOne()
            .HasForeignKey(v => v.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
